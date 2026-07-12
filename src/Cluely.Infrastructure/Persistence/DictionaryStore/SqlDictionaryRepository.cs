using Cluely.Application.Common.Ports.Content;
using Cluely.Domain.Content.ValueObjects;
using Cluely.Infrastructure.Persistence.Exceptions;
using Cluely.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;
using DictionaryAggregate = Cluely.Domain.Content.Dictionary;

namespace Cluely.Infrastructure.Persistence.DictionaryStore;

/// <summary>
/// Snapshot-based dictionary repository. The aggregate is stored as a row carrying discovery-queryable
/// columns plus a serialized payload; <c>Version</c> is the optimistic-concurrency token, and a unique
/// idempotency key makes create/clone replay deterministic (no duplicate aggregates).
/// </summary>
public sealed class SqlDictionaryRepository : IDictionaryRepository
{
    private readonly CluelyDbContext _dbContext;

    public SqlDictionaryRepository(CluelyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DictionaryAggregate?> GetAsync(DictionaryId id, CancellationToken cancellationToken = default)
    {
        var snapshot = await _dbContext.DictionarySnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DictionaryId == id.Value, cancellationToken);

        return snapshot is null ? null : Recover(snapshot);
    }

    public async Task<DictionaryAggregate?> GetByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _dbContext.DictionarySnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.IdempotencyKey == idempotencyKey, cancellationToken);

        return snapshot is null ? null : Recover(snapshot);
    }

    public async Task AddAsync(
        DictionaryAggregate dictionary,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var snapshot = DictionaryMapper.ToSnapshot(dictionary, idempotencyKey, DateTime.UtcNow);
        _dbContext.DictionarySnapshots.Add(snapshot);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // A unique-key violation means the aggregate (or its idempotency key) already exists.
            throw new DictionaryPersistenceException(
                $"Failed to add dictionary {dictionary.Id.Value}.", ex);
        }
    }

    public async Task UpdateAsync(DictionaryAggregate dictionary, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.DictionarySnapshots
            .Include(d => d.ShareGrants)
            .FirstOrDefaultAsync(d => d.DictionaryId == dictionary.Id.Value, cancellationToken);

        if (existing is null)
        {
            throw new DictionaryPersistenceException(
                $"Dictionary {dictionary.Id.Value} was not found for update.");
        }

        if (existing.Version != dictionary.Version.Value - 1)
        {
            throw new DictionaryConcurrencyException(
                $"Version conflict for dictionary {dictionary.Id.Value}: stored {existing.Version}, expected {dictionary.Version.Value - 1}.");
        }

        var updated = DictionaryMapper.ToSnapshot(dictionary, existing.IdempotencyKey, existing.CreatedAt);
        _dbContext.Entry(existing).CurrentValues.SetValues(updated);

        existing.ShareGrants.Clear();
        foreach (var grant in updated.ShareGrants)
        {
            existing.ShareGrants.Add(grant);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DictionaryConcurrencyException(
                $"Concurrent update detected for dictionary {dictionary.Id.Value}.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new DictionaryPersistenceException(
                $"Failed to update dictionary {dictionary.Id.Value}.", ex);
        }
    }

    private static DictionaryAggregate Recover(Models.DictionarySnapshot snapshot)
    {
        try
        {
            return DictionaryMapper.ToDomain(snapshot);
        }
        catch (Exception ex)
        {
            throw new DictionaryPersistenceException(
                $"Failed to recover dictionary {snapshot.DictionaryId} from snapshot.", ex);
        }
    }
}

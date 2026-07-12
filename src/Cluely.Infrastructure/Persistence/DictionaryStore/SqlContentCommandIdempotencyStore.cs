using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Content.PublishDictionary;
using Microsoft.EntityFrameworkCore;

namespace Cluely.Infrastructure.Persistence.DictionaryStore;

public sealed class SqlContentCommandIdempotencyStore : IContentCommandIdempotencyStore
{
    private const string PublishCommandName = "PublishDictionary";

    private readonly CluelyDbContext _dbContext;

    public SqlContentCommandIdempotencyStore(CluelyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PublishDictionaryResult?> TryGetPublishOutcomeAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var outcome = await _dbContext.ContentCommandOutcomes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.IdempotencyKey == idempotencyKey && row.CommandName == PublishCommandName,
                cancellationToken);

        return outcome is null
            ? null
            : new PublishDictionaryResult(
                outcome.DictionaryId,
                outcome.VersionId,
                outcome.VersionLabel,
                outcome.WordCount);
    }

    public async Task SavePublishOutcomeAsync(
        Guid idempotencyKey,
        PublishDictionaryResult outcome,
        CancellationToken cancellationToken = default)
    {
        var entity = new Models.ContentCommandOutcome
        {
            IdempotencyKey = idempotencyKey,
            CommandName = PublishCommandName,
            DictionaryId = outcome.DictionaryId,
            VersionId = outcome.VersionId,
            VersionLabel = outcome.VersionLabel,
            WordCount = outcome.WordCount,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.ContentCommandOutcomes.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(entity).State = EntityState.Detached;
        }
    }
}

using Cluely.Application.Common.Ports;
using Cluely.Domain.Room;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Persistence.Exceptions;
using Cluely.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Cluely.Infrastructure.Persistence.RoomCustody;

public sealed class SqlRoomCustody : IRoomCustody
{
    private readonly CluelyDbContext _dbContext;

    public SqlRoomCustody(CluelyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Room?> GetAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await RecoverRoomAsync(roomId.Value, cancellationToken);
    }

    public async Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken = default)
    {
        var snapshot = await _dbContext.RoomSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(rs => rs.RoomCode == roomCode.Value, cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        return await RecoverRoomAsync(snapshot.RoomId, cancellationToken);
    }

    public async Task SaveAsync(Room room, CancellationToken cancellationToken = default)
    {
        var pendingEvents = room.GetPendingEvents();
        if (pendingEvents.Count == 0)
        {
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = DateTime.UtcNow;
            var existingSnapshot = await _dbContext.RoomSnapshots
                .FirstOrDefaultAsync(rs => rs.RoomId == room.Id.Value, cancellationToken);

            if (existingSnapshot is null)
            {
                _dbContext.RoomSnapshots.Add(room.ToSnapshotEntity(now));
            }
            else
            {
                ValidateExpectedVersion(existingSnapshot.Version, room.Version.Value);

                var updatedSnapshot = room.ToSnapshotEntity(existingSnapshot.CreatedAt);
                _dbContext.Entry(existingSnapshot).CurrentValues.SetValues(updatedSnapshot);
            }

            var nextSequence = await GetNextEventSequenceAsync(room.Id.Value, cancellationToken);
            var eventEntities = room.ToEventEntities(nextSequence, now);
            if (eventEntities.Count > 0)
            {
                _dbContext.RoomEvents.AddRange(eventEntities);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (RoomCustodyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new RoomCustodyException($"Failed to persist room {room.Id.Value}.", ex);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new RoomCustodyException($"Unexpected persistence failure for room {room.Id.Value}.", ex);
        }
    }

    private async Task<Room?> RecoverRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var snapshot = await _dbContext.RoomSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(rs => rs.RoomId == roomId, cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        Room room;
        try
        {
            room = snapshot.ToDomain();
        }
        catch (RoomCustodyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RoomCustodyException($"Failed to recover room {roomId} from snapshot.", ex);
        }

        var tailEvents = await _dbContext.RoomEvents
            .AsNoTracking()
            .Where(re => re.RoomId == roomId && re.AggregateVersion > snapshot.Version)
            .OrderBy(re => re.Sequence)
            .ToListAsync(cancellationToken);

        if (tailEvents.Count > 0)
        {
            throw new RoomCustodyException(
                $"Room {roomId} has {tailEvents.Count} committed event(s) after snapshot version {snapshot.Version}. Replay is required but custody is inconsistent.");
        }

        return room;
    }

    private static void ValidateExpectedVersion(int storedVersion, int committedVersion)
    {
        if (storedVersion != committedVersion - 1)
        {
            throw new RoomCustodyException(
                $"Version conflict: expected stored version {committedVersion - 1} but found {storedVersion}.");
        }
    }

    private async Task<long> GetNextEventSequenceAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var maxSequence = await _dbContext.RoomEvents
            .Where(re => re.RoomId == roomId)
            .Select(re => (long?)re.Sequence)
            .MaxAsync(cancellationToken);

        return maxSequence ?? 0;
    }
}

using Cluely.Domain.Room;
using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Application.Common.Ports;

public interface IRoomCustody
{
    Task<Room?> GetAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken = default);
    Task SaveAsync(Room room, CancellationToken cancellationToken = default);
}
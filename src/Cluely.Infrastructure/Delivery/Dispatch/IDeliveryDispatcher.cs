using Cluely.Domain.Room;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Delivery.Contracts;

namespace Cluely.Infrastructure.Delivery.Dispatch;

public interface IDeliveryDispatcher
{
    Task SendSnapshotAsync(
        string connectionId,
        Room room,
        Role role,
        Team team,
        CancellationToken cancellationToken = default);

    Task BroadcastUpdateAsync(Room room, CancellationToken cancellationToken = default);
}

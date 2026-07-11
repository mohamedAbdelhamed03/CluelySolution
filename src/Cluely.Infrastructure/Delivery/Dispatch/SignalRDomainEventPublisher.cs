using Cluely.Application.Common.Ports;
using Cluely.Domain.Common;
using Cluely.Infrastructure.Delivery.Dispatch;
using Microsoft.Extensions.Logging;

namespace Cluely.Infrastructure.Delivery.Dispatch;

public sealed class SignalRDomainEventPublisher : IDomainEventPublisher
{
    private readonly IRoomCustody _roomCustody;
    private readonly IDeliveryDispatcher _deliveryDispatcher;
    private readonly ILogger<SignalRDomainEventPublisher> _logger;

    public SignalRDomainEventPublisher(
        IRoomCustody roomCustody,
        IDeliveryDispatcher deliveryDispatcher,
        ILogger<SignalRDomainEventPublisher> logger)
    {
        _roomCustody = roomCustody;
        _deliveryDispatcher = deliveryDispatcher;
        _logger = logger;
    }

    public async Task PublishAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return;
        }

        if (events[0] is not IRoomDomainEvent roomEvent)
        {
            throw new InvalidOperationException(
                $"Domain event '{events[0].GetType().Name}' does not implement {nameof(IRoomDomainEvent)}.");
        }

        var room = await _roomCustody.GetAsync(roomEvent.RoomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning(
                "Skipping delivery for room {RoomId}: committed room not found in custody after publish.",
                roomEvent.RoomId.Value);
            return;
        }

        _logger.LogDebug(
            "Broadcasting committed update for room {RoomId} at version {Version}.",
            room.Id.Value,
            room.Version.Value);

        await _deliveryDispatcher.BroadcastUpdateAsync(room, cancellationToken);
    }
}

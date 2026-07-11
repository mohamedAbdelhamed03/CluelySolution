using Cluely.Application.Common.Ports;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Delivery.Connections;
using Cluely.Infrastructure.Delivery.Dispatch;
using Cluely.Infrastructure.Delivery.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Cluely.Infrastructure.Delivery;

public interface IGameConnectionService
{
    Task JoinRoomAsync(string connectionId, Guid roomId, Guid participantId, CancellationToken cancellationToken = default);

    Task LeaveRoomAsync(string connectionId, CancellationToken cancellationToken = default);

    Task OnDisconnectedAsync(string connectionId, CancellationToken cancellationToken = default);
}

public sealed class GameConnectionService : IGameConnectionService
{
    private readonly IRoomCustody _roomCustody;
    private readonly IConnectionRegistry _connectionRegistry;
    private readonly IDeliveryDispatcher _deliveryDispatcher;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameConnectionService> _logger;

    public GameConnectionService(
        IRoomCustody roomCustody,
        IConnectionRegistry connectionRegistry,
        IDeliveryDispatcher deliveryDispatcher,
        IHubContext<GameHub> hubContext,
        ILogger<GameConnectionService> logger)
    {
        _roomCustody = roomCustody;
        _connectionRegistry = connectionRegistry;
        _deliveryDispatcher = deliveryDispatcher;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task JoinRoomAsync(
        string connectionId,
        Guid roomId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        var room = await _roomCustody.GetAsync(RoomId.From(roomId), cancellationToken);
        if (room is null)
        {
            _logger.LogWarning(
                "Join rejected for connection {ConnectionId}: room {RoomId} not found.",
                connectionId,
                roomId);
            await SendErrorAsync(connectionId, "RoomNotFound", cancellationToken);
            return;
        }

        var participant = room.Participants.SingleOrDefault(p => p.Id.Value == participantId);
        if (participant is null)
        {
            _logger.LogWarning(
                "Join rejected for connection {ConnectionId}: participant {ParticipantId} not in room {RoomId}.",
                connectionId,
                participantId,
                roomId);
            await SendErrorAsync(connectionId, "ParticipantNotFound", cancellationToken);
            return;
        }

        await _hubContext.Groups.AddToGroupAsync(connectionId, GameHub.RoomGroupName(roomId), cancellationToken);

        _connectionRegistry.Register(new RoomConnection(
            connectionId,
            roomId,
            participantId,
            participant.Role.Value,
            participant.Team.Value));

        await _deliveryDispatcher.SendSnapshotAsync(
            connectionId,
            room,
            participant.Role,
            participant.Team,
            cancellationToken);
    }

    public async Task LeaveRoomAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        var connection = _connectionRegistry.GetByConnectionId(connectionId);
        if (connection is null)
        {
            return;
        }

        await _hubContext.Groups.RemoveFromGroupAsync(
            connectionId,
            GameHub.RoomGroupName(connection.RoomId),
            cancellationToken);

        _connectionRegistry.Remove(connectionId);
    }

    public async Task OnDisconnectedAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        var connection = _connectionRegistry.GetByConnectionId(connectionId);
        if (connection is not null)
        {
            await _hubContext.Groups.RemoveFromGroupAsync(
                connectionId,
                GameHub.RoomGroupName(connection.RoomId),
                cancellationToken);
        }

        _connectionRegistry.Remove(connectionId);
    }

    private Task SendErrorAsync(string connectionId, string code, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Client(connectionId)
            .SendAsync(DeliveryHubMethods.DeliveryError, code, cancellationToken);
    }
}

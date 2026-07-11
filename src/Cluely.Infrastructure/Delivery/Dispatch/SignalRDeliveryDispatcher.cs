using Cluely.Domain.Room;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Common;
using Cluely.Infrastructure.Delivery.Connections;
using Cluely.Infrastructure.Delivery.Contracts;
using Cluely.Infrastructure.Delivery.Hubs;
using Cluely.Infrastructure.Delivery.Projections;
using Cluely.Infrastructure.Delivery.Visibility;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Cluely.Infrastructure.Delivery.Dispatch;

public sealed class SignalRDeliveryDispatcher : IDeliveryDispatcher
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IConnectionRegistry _connectionRegistry;
    private readonly IProjectionBuilder _projectionBuilder;
    private readonly IVisibilityFilter _visibilityFilter;
    private readonly ILogger<SignalRDeliveryDispatcher> _logger;

    public SignalRDeliveryDispatcher(
        IHubContext<GameHub> hubContext,
        IConnectionRegistry connectionRegistry,
        IProjectionBuilder projectionBuilder,
        IVisibilityFilter visibilityFilter,
        ILogger<SignalRDeliveryDispatcher> logger)
    {
        _hubContext = hubContext;
        _connectionRegistry = connectionRegistry;
        _projectionBuilder = projectionBuilder;
        _visibilityFilter = visibilityFilter;
        _logger = logger;
    }

    public async Task SendSnapshotAsync(
        string connectionId,
        Room room,
        Role role,
        Team team,
        CancellationToken cancellationToken = default)
    {
        var envelope = CreateEnvelope(room, role, team, isSnapshot: true);
        await _hubContext.Clients.Client(connectionId)
            .SendAsync(DeliveryHubMethods.ReceiveSnapshot, envelope, cancellationToken);
    }

    public async Task BroadcastUpdateAsync(Room room, CancellationToken cancellationToken = default)
    {
        var connections = _connectionRegistry.GetRoomConnections(room.Id.Value);
        if (connections.Count == 0)
        {
            _logger.LogDebug(
                "No active connections for room {RoomId}; skipping broadcast.",
                room.Id.Value);
            return;
        }

        var internalProjection = _projectionBuilder.Build(room);

        foreach (var connection in connections)
        {
            var role = Role.From(connection.Role);
            var team = TeamParsing.FromStoredValue(connection.Team);
            var filteredProjection = _visibilityFilter.Filter(internalProjection, role, team);
            var envelope = new DeliveryEnvelope<RoomProjectionDto>(
                room.Id.Value,
                room.Version.Value,
                IsSnapshot: false,
                filteredProjection);

            try
            {
                await _hubContext.Clients.Client(connection.ConnectionId)
                    .SendAsync(DeliveryHubMethods.ReceiveUpdate, envelope, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to deliver update to connection {ConnectionId} in room {RoomId}.",
                    connection.ConnectionId,
                    room.Id.Value);
            }
        }
    }

    private DeliveryEnvelope<RoomProjectionDto> CreateEnvelope(
        Room room,
        Role role,
        Team team,
        bool isSnapshot)
    {
        var internalProjection = _projectionBuilder.Build(room);
        var filteredProjection = _visibilityFilter.Filter(internalProjection, role, team);

        return new DeliveryEnvelope<RoomProjectionDto>(
            room.Id.Value,
            room.Version.Value,
            isSnapshot,
            filteredProjection);
    }
}

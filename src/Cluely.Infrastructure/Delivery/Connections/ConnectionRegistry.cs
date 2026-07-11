using System.Collections.Concurrent;

namespace Cluely.Infrastructure.Delivery.Connections;

/// <summary>
/// In-memory, single-node connection registry (ADR-007).
/// Tracks active SignalR connections per room and participant.
/// Not suitable for multi-instance deployment without a future backplane.
/// </summary>
public sealed class ConnectionRegistry : IConnectionRegistry
{
    private readonly ConcurrentDictionary<string, RoomConnection> _connections = new();

    public void Register(RoomConnection connection)
    {
        _connections[connection.ConnectionId] = connection;
    }

    public bool Remove(string connectionId)
    {
        return _connections.TryRemove(connectionId, out _);
    }

    public RoomConnection? GetByConnectionId(string connectionId)
    {
        return _connections.TryGetValue(connectionId, out var connection) ? connection : null;
    }

    public IReadOnlyList<RoomConnection> GetRoomConnections(Guid roomId)
    {
        return _connections.Values
            .Where(connection => connection.RoomId == roomId)
            .ToList();
    }

    public IReadOnlyList<RoomConnection> GetParticipantConnections(Guid roomId, Guid participantId)
    {
        return _connections.Values
            .Where(connection => connection.RoomId == roomId && connection.ParticipantId == participantId)
            .ToList();
    }
}

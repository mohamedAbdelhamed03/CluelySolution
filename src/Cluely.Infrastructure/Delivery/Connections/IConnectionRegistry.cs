namespace Cluely.Infrastructure.Delivery.Connections;

public interface IConnectionRegistry
{
    void Register(RoomConnection connection);

    bool Remove(string connectionId);

    RoomConnection? GetByConnectionId(string connectionId);

    IReadOnlyList<RoomConnection> GetRoomConnections(Guid roomId);

    IReadOnlyList<RoomConnection> GetParticipantConnections(Guid roomId, Guid participantId);
}

namespace Cluely.Infrastructure.Delivery.Connections;

public sealed record RoomConnection(
    string ConnectionId,
    Guid RoomId,
    Guid ParticipantId,
    string Role,
    string Team);

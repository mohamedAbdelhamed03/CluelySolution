namespace Cluely.Application.Queries.GetRoom;

public sealed record GetRoomQuery(Guid RoomId, Guid CorrelationId);

namespace Cluely.Application.Queries.GetRoomProjection;

public sealed record GetRoomProjectionQuery(Guid RoomId, Guid ParticipantId, Guid CorrelationId);

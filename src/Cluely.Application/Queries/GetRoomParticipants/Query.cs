namespace Cluely.Application.Queries.GetRoomParticipants;

public sealed record GetRoomParticipantsQuery(Guid RoomId, Guid CorrelationId);

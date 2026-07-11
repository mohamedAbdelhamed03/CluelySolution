namespace Cluely.Application.Rooms.AssignTeam;

public sealed record AssignTeamCommand(Guid RoomId, Guid ParticipantId, string Team, Guid CorrelationId);
namespace Cluely.Application.Rooms.AssignRole;

public sealed record AssignRoleCommand(Guid RoomId, Guid ParticipantId, string Role, Guid CorrelationId);
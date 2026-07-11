namespace Cluely.Application.Gameplay.EndTurn;

public sealed record EndTurnCommand(Guid RoomId, Guid ParticipantId, Guid CorrelationId);
namespace Cluely.Application.Gameplay.StartMatch;

public sealed record StartMatchCommand(Guid RoomId, Guid ParticipantId, Guid CorrelationId);
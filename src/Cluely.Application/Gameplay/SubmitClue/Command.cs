namespace Cluely.Application.Gameplay.SubmitClue;

public sealed record SubmitClueCommand(Guid RoomId, Guid ParticipantId, string Word, int Count, Guid CorrelationId);
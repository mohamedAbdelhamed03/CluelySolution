namespace Cluely.Application.Gameplay.SubmitGuess;

public sealed record SubmitGuessCommand(Guid RoomId, Guid ParticipantId, int CardPosition, Guid CorrelationId);
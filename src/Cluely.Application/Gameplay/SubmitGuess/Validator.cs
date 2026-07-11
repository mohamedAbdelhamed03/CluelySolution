using FluentValidation;

namespace Cluely.Application.Gameplay.SubmitGuess;

public sealed class SubmitGuessCommandValidator : AbstractValidator<SubmitGuessCommand>
{
    public SubmitGuessCommandValidator()
    {
        RuleFor(c => c.RoomId).NotEmpty();
        RuleFor(c => c.ParticipantId).NotEmpty();
        RuleFor(c => c.CardPosition).GreaterThanOrEqualTo(0).LessThanOrEqualTo(24);
        RuleFor(c => c.CorrelationId).NotEmpty();
    }
}
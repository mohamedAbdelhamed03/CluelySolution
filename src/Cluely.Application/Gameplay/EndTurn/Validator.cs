using FluentValidation;

namespace Cluely.Application.Gameplay.EndTurn;

public sealed class EndTurnCommandValidator : AbstractValidator<EndTurnCommand>
{
    public EndTurnCommandValidator()
    {
        RuleFor(c => c.RoomId).NotEmpty();
        RuleFor(c => c.ParticipantId).NotEmpty();
        RuleFor(c => c.CorrelationId).NotEmpty();
    }
}
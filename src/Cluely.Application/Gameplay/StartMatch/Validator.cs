using FluentValidation;

namespace Cluely.Application.Gameplay.StartMatch;

public sealed class StartMatchCommandValidator : AbstractValidator<StartMatchCommand>
{
    public StartMatchCommandValidator()
    {
        RuleFor(c => c.RoomId).NotEmpty();
        RuleFor(c => c.ParticipantId).NotEmpty();
        RuleFor(c => c.CorrelationId).NotEmpty();
    }
}
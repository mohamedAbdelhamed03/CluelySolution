using FluentValidation;

namespace Cluely.Application.Gameplay.SubmitClue;

public sealed class SubmitClueCommandValidator : AbstractValidator<SubmitClueCommand>
{
    public SubmitClueCommandValidator()
    {
        RuleFor(c => c.RoomId).NotEmpty();
        RuleFor(c => c.ParticipantId).NotEmpty();
        RuleFor(c => c.Word).NotEmpty();
        RuleFor(c => c.Count).GreaterThan(0).LessThanOrEqualTo(9);
        RuleFor(c => c.CorrelationId).NotEmpty();
    }
}
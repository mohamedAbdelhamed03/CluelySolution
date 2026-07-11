using FluentValidation;

namespace Cluely.Application.Rooms.AssignTeam;

public sealed class AssignTeamCommandValidator : AbstractValidator<AssignTeamCommand>
{
    public AssignTeamCommandValidator()
    {
        RuleFor(c => c.RoomId).NotEmpty();
        RuleFor(c => c.ParticipantId).NotEmpty();
        RuleFor(c => c.Team).NotEmpty().Must(t => t.Equals("Red", StringComparison.OrdinalIgnoreCase) ||
                                                   t.Equals("Blue", StringComparison.OrdinalIgnoreCase) ||
                                                   t.Equals("Unassigned", StringComparison.OrdinalIgnoreCase));
        RuleFor(c => c.CorrelationId).NotEmpty();
    }
}
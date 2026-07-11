using FluentValidation;

namespace Cluely.Application.Rooms.AssignRole;

public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(c => c.RoomId).NotEmpty();
        RuleFor(c => c.ParticipantId).NotEmpty();
        RuleFor(c => c.Role).NotEmpty().Must(r => r.Equals("Spymaster", StringComparison.OrdinalIgnoreCase) ||
                                                   r.Equals("Operative", StringComparison.OrdinalIgnoreCase));
        RuleFor(c => c.CorrelationId).NotEmpty();
    }
}
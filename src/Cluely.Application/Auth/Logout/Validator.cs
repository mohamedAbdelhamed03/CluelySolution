using FluentValidation;

namespace Cluely.Application.Auth.Logout;

public sealed class LogoutUserCommandValidator : AbstractValidator<LogoutUserCommand>
{
    public LogoutUserCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
        RuleFor(command => command.CorrelationId).NotEmpty();
    }
}

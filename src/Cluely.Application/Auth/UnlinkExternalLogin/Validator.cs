using Cluely.Application.Common.Ports.Identity;
using FluentValidation;

namespace Cluely.Application.Auth.UnlinkExternalLogin;

public sealed class UnlinkExternalLoginCommandValidator : AbstractValidator<UnlinkExternalLoginCommand>
{
    public UnlinkExternalLoginCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Provider)
            .NotEmpty()
            .Must(provider => ExternalAuthProviders.Google.Equals(provider, StringComparison.OrdinalIgnoreCase)
                || ExternalAuthProviders.Facebook.Equals(provider, StringComparison.OrdinalIgnoreCase)
                || ExternalAuthProviders.Apple.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Provider must be one of: google, facebook, apple.");
        RuleFor(command => command.CorrelationId).NotEmpty();
    }
}

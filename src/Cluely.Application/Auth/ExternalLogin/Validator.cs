using Cluely.Application.Common.Ports.Identity;
using FluentValidation;

namespace Cluely.Application.Auth.ExternalLogin;

public sealed class ExternalLoginCommandValidator : AbstractValidator<ExternalLoginCommand>
{
    public ExternalLoginCommandValidator()
    {
        RuleFor(command => command.Provider)
            .NotEmpty()
            .Must(provider => ExternalAuthProviders.Google.Equals(provider, StringComparison.OrdinalIgnoreCase)
                || ExternalAuthProviders.Facebook.Equals(provider, StringComparison.OrdinalIgnoreCase)
                || ExternalAuthProviders.Apple.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Provider must be one of: google, facebook, apple.");

        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.CorrelationId).NotEmpty();
    }
}

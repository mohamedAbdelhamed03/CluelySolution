using FluentValidation;

namespace Cluely.Application.Content.RevokeShare;

public sealed class RevokeShareCommandValidator : AbstractValidator<RevokeShareCommand>
{
    public RevokeShareCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.GranteeId).NotEmpty().WithMessage("Grantee ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

using FluentValidation;

namespace Cluely.Application.Content.ShareDictionary;

public sealed class ShareDictionaryCommandValidator : AbstractValidator<ShareDictionaryCommand>
{
    public ShareDictionaryCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.GranteeId).NotEmpty().WithMessage("Grantee ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

using FluentValidation;

namespace Cluely.Application.Content.ValidateDraft;

public sealed class ValidateDraftCommandValidator : AbstractValidator<ValidateDraftCommand>
{
    public ValidateDraftCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

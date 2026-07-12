using FluentValidation;

namespace Cluely.Application.Content.CancelDeleteDictionary;

public sealed class CancelDeleteDictionaryCommandValidator : AbstractValidator<CancelDeleteDictionaryCommand>
{
    public CancelDeleteDictionaryCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

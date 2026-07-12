using FluentValidation;

namespace Cluely.Application.Content.RestoreDictionary;

public sealed class RestoreDictionaryCommandValidator : AbstractValidator<RestoreDictionaryCommand>
{
    public RestoreDictionaryCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

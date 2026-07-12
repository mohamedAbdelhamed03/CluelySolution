using FluentValidation;

namespace Cluely.Application.Content.ReportDictionary;

public sealed class ReportDictionaryCommandValidator : AbstractValidator<ReportDictionaryCommand>
{
    public ReportDictionaryCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

using FluentValidation;

namespace Cluely.Application.Content.BulkAddWords;

public sealed class BulkAddWordsCommandValidator : AbstractValidator<BulkAddWordsCommand>
{
    public BulkAddWordsCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.Words).NotNull().WithMessage("Words are required.");
        RuleFor(c => c.Words).NotEmpty().WithMessage("At least one word is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

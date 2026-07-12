using FluentValidation;

namespace Cluely.Application.Content.ArchiveDictionary;

public sealed class ArchiveDictionaryCommandValidator : AbstractValidator<ArchiveDictionaryCommand>
{
    public ArchiveDictionaryCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

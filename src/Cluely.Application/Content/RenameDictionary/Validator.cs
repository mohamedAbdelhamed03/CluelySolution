using FluentValidation;

namespace Cluely.Application.Content.RenameDictionary;

public sealed class RenameDictionaryCommandValidator : AbstractValidator<RenameDictionaryCommand>
{
    public RenameDictionaryCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.Title).NotEmpty().WithMessage("Title is required.");
        RuleFor(c => c.Description).NotNull().WithMessage("Description is required.");
        RuleFor(c => c.Language).NotEmpty().WithMessage("Language is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

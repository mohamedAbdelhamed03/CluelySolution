using FluentValidation;

namespace Cluely.Application.Content.ReplaceWord;

public sealed class ReplaceWordCommandValidator : AbstractValidator<ReplaceWordCommand>
{
    public ReplaceWordCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.ExistingWord).NotEmpty().WithMessage("Existing word is required.");
        RuleFor(c => c.NewWord).NotEmpty().WithMessage("New word is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

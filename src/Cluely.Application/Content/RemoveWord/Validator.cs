using FluentValidation;

namespace Cluely.Application.Content.RemoveWord;

public sealed class RemoveWordCommandValidator : AbstractValidator<RemoveWordCommand>
{
    public RemoveWordCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.Word).NotEmpty().WithMessage("Word is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

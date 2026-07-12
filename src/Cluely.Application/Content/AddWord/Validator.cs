using FluentValidation;

namespace Cluely.Application.Content.AddWord;

public sealed class AddWordCommandValidator : AbstractValidator<AddWordCommand>
{
    public AddWordCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.Word).NotEmpty().WithMessage("Word is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

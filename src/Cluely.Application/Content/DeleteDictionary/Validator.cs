using FluentValidation;

namespace Cluely.Application.Content.DeleteDictionary;

public sealed class DeleteDictionaryCommandValidator : AbstractValidator<DeleteDictionaryCommand>
{
    public DeleteDictionaryCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

using FluentValidation;

namespace Cluely.Application.Content.PublishDictionary;

public sealed class PublishDictionaryCommandValidator : AbstractValidator<PublishDictionaryCommand>
{
    public PublishDictionaryCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
        RuleFor(c => c.IdempotencyKey).NotEmpty().WithMessage("Idempotency key is required.");
    }
}

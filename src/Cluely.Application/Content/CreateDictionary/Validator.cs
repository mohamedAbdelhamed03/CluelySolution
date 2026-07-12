using FluentValidation;

namespace Cluely.Application.Content.CreateDictionary;

public sealed class CreateDictionaryCommandValidator : AbstractValidator<CreateDictionaryCommand>
{
    public CreateDictionaryCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().WithMessage("Title is required.");
        RuleFor(c => c.Description).NotNull().WithMessage("Description is required.");
        RuleFor(c => c.Language).NotEmpty().WithMessage("Language is required.");
        RuleFor(c => c.ContentType).NotEmpty().WithMessage("Content type is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
        RuleFor(c => c.IdempotencyKey).NotEmpty().WithMessage("Idempotency key is required.");
    }
}

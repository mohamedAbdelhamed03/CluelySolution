using FluentValidation;

namespace Cluely.Application.Content.CloneDictionary;

public sealed class CloneDictionaryCommandValidator : AbstractValidator<CloneDictionaryCommand>
{
    public CloneDictionaryCommandValidator()
    {
        RuleFor(c => c.SourceDictionaryId).NotEmpty().WithMessage("Source dictionary ID is required.");
        RuleFor(c => c.SourceVersionId).NotEmpty().WithMessage("Source version ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
        RuleFor(c => c.IdempotencyKey).NotEmpty().WithMessage("Idempotency key is required.");
    }
}

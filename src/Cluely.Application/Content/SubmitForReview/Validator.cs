using FluentValidation;

namespace Cluely.Application.Content.SubmitForReview;

public sealed class SubmitForReviewCommandValidator : AbstractValidator<SubmitForReviewCommand>
{
    public SubmitForReviewCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.VersionId).NotEmpty().WithMessage("Version ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

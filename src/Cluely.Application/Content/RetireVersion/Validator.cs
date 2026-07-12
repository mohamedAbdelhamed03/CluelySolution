using FluentValidation;

namespace Cluely.Application.Content.RetireVersion;

public sealed class RetireVersionCommandValidator : AbstractValidator<RetireVersionCommand>
{
    public RetireVersionCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.VersionId).NotEmpty().WithMessage("Version ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

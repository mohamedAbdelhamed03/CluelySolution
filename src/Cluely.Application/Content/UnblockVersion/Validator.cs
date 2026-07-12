using FluentValidation;

namespace Cluely.Application.Content.UnblockVersion;

public sealed class UnblockVersionCommandValidator : AbstractValidator<UnblockVersionCommand>
{
    public UnblockVersionCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.VersionId).NotEmpty().WithMessage("Version ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

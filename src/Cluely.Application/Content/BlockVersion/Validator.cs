using FluentValidation;

namespace Cluely.Application.Content.BlockVersion;

public sealed class BlockVersionCommandValidator : AbstractValidator<BlockVersionCommand>
{
    public BlockVersionCommandValidator()
    {
        RuleFor(c => c.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(c => c.VersionId).NotEmpty().WithMessage("Version ID is required.");
        RuleFor(c => c.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

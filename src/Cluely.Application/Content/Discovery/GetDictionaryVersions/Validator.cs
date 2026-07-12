using FluentValidation;

namespace Cluely.Application.Content.Discovery.GetDictionaryVersions;

public sealed class GetDictionaryVersionsQueryValidator : AbstractValidator<GetDictionaryVersionsQuery>
{
    public GetDictionaryVersionsQueryValidator()
    {
        RuleFor(query => query.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(query => query.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

using FluentValidation;

namespace Cluely.Application.Content.Discovery.GetDictionaryDetails;

public sealed class GetDictionaryDetailsQueryValidator : AbstractValidator<GetDictionaryDetailsQuery>
{
    public GetDictionaryDetailsQueryValidator()
    {
        RuleFor(query => query.DictionaryId).NotEmpty().WithMessage("Dictionary ID is required.");
        RuleFor(query => query.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

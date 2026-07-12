using FluentValidation;

namespace Cluely.Application.Content.Discovery.GetMyDictionaries;

public sealed class GetMyDictionariesQueryValidator : AbstractValidator<GetMyDictionariesQuery>
{
    public GetMyDictionariesQueryValidator()
    {
        RuleFor(query => query.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

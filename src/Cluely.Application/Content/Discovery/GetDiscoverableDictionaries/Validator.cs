using FluentValidation;

namespace Cluely.Application.Content.Discovery.GetDiscoverableDictionaries;

public sealed class GetDiscoverableDictionariesQueryValidator : AbstractValidator<GetDiscoverableDictionariesQuery>
{
    public GetDiscoverableDictionariesQueryValidator()
    {
        RuleFor(query => query.CorrelationId).NotEmpty().WithMessage("Correlation ID is required.");
    }
}

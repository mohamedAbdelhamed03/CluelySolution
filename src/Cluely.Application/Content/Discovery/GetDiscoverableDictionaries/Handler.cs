using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.Discovery.GetDiscoverableDictionaries;

public sealed class GetDiscoverableDictionariesHandler
{
    private readonly IDictionaryReadModelProvider _readModelProvider;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IValidator<GetDiscoverableDictionariesQuery> _validator;

    public GetDiscoverableDictionariesHandler(
        IDictionaryReadModelProvider readModelProvider,
        ICurrentUserAccessor currentUserAccessor,
        IValidator<GetDiscoverableDictionariesQuery> validator)
    {
        _readModelProvider = readModelProvider;
        _currentUserAccessor = currentUserAccessor;
        _validator = validator;
    }

    public async Task<Result<GetDiscoverableDictionariesResult>> HandleAsync(
        GetDiscoverableDictionariesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<GetDiscoverableDictionariesResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<GetDiscoverableDictionariesResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var dictionaries = await _readModelProvider.GetDiscoverableAsync(OwnerId.From(userId), cancellationToken);

        return Result.Success(new GetDiscoverableDictionariesResult(dictionaries));
    }
}

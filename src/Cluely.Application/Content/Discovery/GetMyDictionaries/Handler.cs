using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.Discovery.GetMyDictionaries;

public sealed class GetMyDictionariesHandler
{
    private readonly IDictionaryReadModelProvider _readModelProvider;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IValidator<GetMyDictionariesQuery> _validator;

    public GetMyDictionariesHandler(
        IDictionaryReadModelProvider readModelProvider,
        ICurrentUserAccessor currentUserAccessor,
        IValidator<GetMyDictionariesQuery> validator)
    {
        _readModelProvider = readModelProvider;
        _currentUserAccessor = currentUserAccessor;
        _validator = validator;
    }

    public async Task<Result<GetMyDictionariesResult>> HandleAsync(
        GetMyDictionariesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<GetMyDictionariesResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<GetMyDictionariesResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var dictionaries = await _readModelProvider.GetOwnedAsync(OwnerId.From(userId), cancellationToken);

        return Result.Success(new GetMyDictionariesResult(dictionaries));
    }
}

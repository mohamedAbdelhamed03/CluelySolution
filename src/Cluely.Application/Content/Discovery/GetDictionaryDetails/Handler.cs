using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.Discovery.GetDictionaryDetails;

public sealed class GetDictionaryDetailsHandler
{
    private readonly IDictionaryReadModelProvider _readModelProvider;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IValidator<GetDictionaryDetailsQuery> _validator;

    public GetDictionaryDetailsHandler(
        IDictionaryReadModelProvider readModelProvider,
        ICurrentUserAccessor currentUserAccessor,
        IValidator<GetDictionaryDetailsQuery> validator)
    {
        _readModelProvider = readModelProvider;
        _currentUserAccessor = currentUserAccessor;
        _validator = validator;
    }

    public async Task<Result<GetDictionaryDetailsResult>> HandleAsync(
        GetDictionaryDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<GetDictionaryDetailsResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<GetDictionaryDetailsResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var details = await _readModelProvider.GetDetailsAsync(
            DictionaryId.From(query.DictionaryId),
            OwnerId.From(userId),
            cancellationToken);

        if (details is null)
        {
            return Result.Failure<GetDictionaryDetailsResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        return Result.Success(new GetDictionaryDetailsResult(details));
    }
}

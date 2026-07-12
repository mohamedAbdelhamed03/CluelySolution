using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.Discovery.GetDictionaryVersions;

public sealed class GetDictionaryVersionsHandler
{
    private readonly IDictionaryReadModelProvider _readModelProvider;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IValidator<GetDictionaryVersionsQuery> _validator;

    public GetDictionaryVersionsHandler(
        IDictionaryReadModelProvider readModelProvider,
        ICurrentUserAccessor currentUserAccessor,
        IValidator<GetDictionaryVersionsQuery> validator)
    {
        _readModelProvider = readModelProvider;
        _currentUserAccessor = currentUserAccessor;
        _validator = validator;
    }

    public async Task<Result<GetDictionaryVersionsResult>> HandleAsync(
        GetDictionaryVersionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<GetDictionaryVersionsResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<GetDictionaryVersionsResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var versions = await _readModelProvider.GetVersionsAsync(
            DictionaryId.From(query.DictionaryId),
            OwnerId.From(userId),
            cancellationToken);

        if (versions is null)
        {
            return Result.Failure<GetDictionaryVersionsResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        return Result.Success(new GetDictionaryVersionsResult(versions));
    }
}

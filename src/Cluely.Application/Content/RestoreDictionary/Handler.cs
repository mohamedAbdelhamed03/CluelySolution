using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.RestoreDictionary;

public sealed class RestoreDictionaryHandler
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IValidator<RestoreDictionaryCommand> _validator;

    public RestoreDictionaryHandler(
        IDictionaryRepository dictionaryRepository,
        IDomainEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor,
        IValidator<RestoreDictionaryCommand> validator)
    {
        _dictionaryRepository = dictionaryRepository;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
        _validator = validator;
    }

    public async Task<Result<RestoreDictionaryResult>> HandleAsync(
        RestoreDictionaryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<RestoreDictionaryResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<RestoreDictionaryResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var dictionaryId = DictionaryId.From(command.DictionaryId);
        var dictionary = await _dictionaryRepository.GetAsync(dictionaryId, cancellationToken);
        if (dictionary is null)
        {
            return Result.Failure<RestoreDictionaryResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        var owner = OwnerId.From(userId);

        try
        {
            dictionary.Restore(owner);
        }
        catch (DomainException ex)
        {
            return Result.Failure<RestoreDictionaryResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<RestoreDictionaryResult>(new UnexpectedError(
                "UnexpectedError",
                "An unexpected error occurred.",
                ex));
        }

        await _dictionaryRepository.UpdateAsync(dictionary, cancellationToken);
        await _eventPublisher.PublishAsync(dictionary.GetPendingEvents(), cancellationToken);
        dictionary.ClearPendingEvents();

        return Result.Success(new RestoreDictionaryResult(command.DictionaryId));
    }
}

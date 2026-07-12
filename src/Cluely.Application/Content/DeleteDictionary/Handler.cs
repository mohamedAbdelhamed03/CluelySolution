using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.DeleteDictionary;

public sealed class DeleteDictionaryHandler
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IValidator<DeleteDictionaryCommand> _validator;

    public DeleteDictionaryHandler(
        IDictionaryRepository dictionaryRepository,
        IDomainEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor,
        IValidator<DeleteDictionaryCommand> validator)
    {
        _dictionaryRepository = dictionaryRepository;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
        _validator = validator;
    }

    public async Task<Result<DeleteDictionaryResult>> HandleAsync(
        DeleteDictionaryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<DeleteDictionaryResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<DeleteDictionaryResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var dictionaryId = DictionaryId.From(command.DictionaryId);
        var dictionary = await _dictionaryRepository.GetAsync(dictionaryId, cancellationToken);
        if (dictionary is null)
        {
            return Result.Failure<DeleteDictionaryResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        var owner = OwnerId.From(userId);

        try
        {
            dictionary.RequestDeletion(owner);
        }
        catch (DomainException ex)
        {
            return Result.Failure<DeleteDictionaryResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<DeleteDictionaryResult>(new UnexpectedError(
                "UnexpectedError",
                "An unexpected error occurred.",
                ex));
        }

        await _dictionaryRepository.UpdateAsync(dictionary, cancellationToken);
        await _eventPublisher.PublishAsync(dictionary.GetPendingEvents(), cancellationToken);
        dictionary.ClearPendingEvents();

        return Result.Success(new DeleteDictionaryResult(command.DictionaryId));
    }
}

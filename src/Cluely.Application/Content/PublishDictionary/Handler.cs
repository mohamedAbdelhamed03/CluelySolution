using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.PublishDictionary;

public sealed class PublishDictionaryHandler
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IContentCommandIdempotencyStore _idempotencyStore;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IValidator<PublishDictionaryCommand> _validator;

    public PublishDictionaryHandler(
        IDictionaryRepository dictionaryRepository,
        IContentCommandIdempotencyStore idempotencyStore,
        IDomainEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor,
        IValidator<PublishDictionaryCommand> validator)
    {
        _dictionaryRepository = dictionaryRepository;
        _idempotencyStore = idempotencyStore;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
        _validator = validator;
    }

    public async Task<Result<PublishDictionaryResult>> HandleAsync(
        PublishDictionaryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<PublishDictionaryResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<PublishDictionaryResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var cachedOutcome = await _idempotencyStore.TryGetPublishOutcomeAsync(
            command.IdempotencyKey,
            cancellationToken);
        if (cachedOutcome is not null)
        {
            if (cachedOutcome.DictionaryId != command.DictionaryId)
            {
                return Result.Failure<PublishDictionaryResult>(new BusinessError(
                    "IdempotencyKeyConflict",
                    "The idempotency key is already associated with a different dictionary."));
            }

            return Result.Success(cachedOutcome);
        }

        var dictionaryId = DictionaryId.From(command.DictionaryId);
        var dictionary = await _dictionaryRepository.GetAsync(dictionaryId, cancellationToken);
        if (dictionary is null)
        {
            return Result.Failure<PublishDictionaryResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        var owner = OwnerId.From(userId);
        var versionId = VersionId.From(command.IdempotencyKey);
        var publishedAt = command.PublishedAt ?? DateTime.UtcNow;

        try
        {
            dictionary.Publish(owner, versionId, publishedAt);
        }
        catch (DomainException ex)
        {
            return Result.Failure<PublishDictionaryResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<PublishDictionaryResult>(new UnexpectedError(
                "UnexpectedError",
                "An unexpected error occurred.",
                ex));
        }

        var publishedEvent = dictionary.GetPendingEvents().OfType<VersionPublished>().Single();
        var outcome = new PublishDictionaryResult(
            command.DictionaryId,
            versionId.Value,
            publishedEvent.Label.Value,
            publishedEvent.WordCount);

        await _dictionaryRepository.UpdateAsync(dictionary, cancellationToken);
        await TryBackfillPublishOutcomeAsync(command.IdempotencyKey, outcome, cancellationToken);
        await _eventPublisher.PublishAsync(dictionary.GetPendingEvents(), cancellationToken);
        dictionary.ClearPendingEvents();

        return Result.Success(outcome);
    }

    private async Task TryBackfillPublishOutcomeAsync(
        Guid idempotencyKey,
        PublishDictionaryResult outcome,
        CancellationToken cancellationToken)
    {
        if (await _idempotencyStore.TryGetPublishOutcomeAsync(idempotencyKey, cancellationToken) is not null)
        {
            return;
        }

        await _idempotencyStore.SavePublishOutcomeAsync(idempotencyKey, outcome, cancellationToken);
    }
}

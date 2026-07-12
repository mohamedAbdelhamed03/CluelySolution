using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;
using DictionaryAggregate = Cluely.Domain.Content.Dictionary;

namespace Cluely.Application.Content.CloneDictionary;

public sealed class CloneDictionaryHandler
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IValidator<CloneDictionaryCommand> _validator;

    public CloneDictionaryHandler(
        IDictionaryRepository dictionaryRepository,
        IDomainEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor,
        IGuidGenerator guidGenerator,
        IValidator<CloneDictionaryCommand> validator)
    {
        _dictionaryRepository = dictionaryRepository;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
        _guidGenerator = guidGenerator;
        _validator = validator;
    }

    public async Task<Result<CloneDictionaryResult>> HandleAsync(
        CloneDictionaryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<CloneDictionaryResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<CloneDictionaryResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var existing = await _dictionaryRepository.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(new CloneDictionaryResult(
                existing.Id.Value, command.SourceDictionaryId, command.SourceVersionId));
        }

        var cloner = OwnerId.From(userId);
        var source = await _dictionaryRepository.GetAsync(
            DictionaryId.From(command.SourceDictionaryId), cancellationToken);

        // A source the cloner may not view is indistinguishable from a missing one (no enumeration).
        if (source is null || !source.IsViewableBy(cloner))
        {
            return Result.Failure<CloneDictionaryResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        // Non-owners may clone only the current published version; other versions are not discoverable.
        var isOwner = source.Owner == cloner;
        if (!isOwner && source.CurrentVersionId?.Value != command.SourceVersionId)
        {
            return Result.Failure<CloneDictionaryResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        var newId = DictionaryId.From(_guidGenerator.Generate());
        DictionaryAggregate clone;

        try
        {
            clone = DictionaryAggregate.CloneFrom(
                newId,
                cloner,
                source,
                VersionId.From(command.SourceVersionId),
                source.Metadata,
                DateTime.UtcNow);
        }
        catch (DomainException ex)
        {
            return Result.Failure<CloneDictionaryResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<CloneDictionaryResult>(new UnexpectedError(
                "UnexpectedError",
                "An unexpected error occurred.",
                ex));
        }

        await _dictionaryRepository.AddAsync(clone, command.IdempotencyKey, cancellationToken);
        await _eventPublisher.PublishAsync(clone.GetPendingEvents(), cancellationToken);
        clone.ClearPendingEvents();

        return Result.Success(new CloneDictionaryResult(
            newId.Value, command.SourceDictionaryId, command.SourceVersionId));
    }
}

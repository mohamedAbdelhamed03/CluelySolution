using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Content;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.CreateDictionary;

public sealed class CreateDictionaryHandler
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IValidator<CreateDictionaryCommand> _validator;

    public CreateDictionaryHandler(
        IDictionaryRepository dictionaryRepository,
        IDomainEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor,
        IGuidGenerator guidGenerator,
        IValidator<CreateDictionaryCommand> validator)
    {
        _dictionaryRepository = dictionaryRepository;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
        _guidGenerator = guidGenerator;
        _validator = validator;
    }

    public async Task<Result<CreateDictionaryResult>> HandleAsync(
        CreateDictionaryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<CreateDictionaryResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<CreateDictionaryResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var existing = await _dictionaryRepository.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(new CreateDictionaryResult(existing.Id.Value, existing.Metadata.Title.Value));
        }

        var owner = OwnerId.From(userId);
        var dictionaryId = DictionaryId.From(_guidGenerator.Generate());
        Dictionary dictionary;

        try
        {
            var metadata = DictionaryMetadata.Create(
                command.Title,
                command.Description,
                command.Tags,
                command.Language,
                command.Region);
            var contentType = ContentType.From(command.ContentType);
            dictionary = Dictionary.Create(dictionaryId, owner, contentType, metadata);
        }
        catch (DomainException ex)
        {
            return Result.Failure<CreateDictionaryResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CreateDictionaryResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<CreateDictionaryResult>(new UnexpectedError(
                "UnexpectedError",
                "An unexpected error occurred.",
                ex));
        }

        await _dictionaryRepository.AddAsync(dictionary, command.IdempotencyKey, cancellationToken);
        await _eventPublisher.PublishAsync(dictionary.GetPendingEvents(), cancellationToken);
        dictionary.ClearPendingEvents();

        return Result.Success(new CreateDictionaryResult(dictionaryId.Value, dictionary.Metadata.Title.Value));
    }
}

using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.RenameDictionary;

public sealed class RenameDictionaryHandler
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IValidator<RenameDictionaryCommand> _validator;

    public RenameDictionaryHandler(
        IDictionaryRepository dictionaryRepository,
        IDomainEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor,
        IValidator<RenameDictionaryCommand> validator)
    {
        _dictionaryRepository = dictionaryRepository;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
        _validator = validator;
    }

    public async Task<Result<RenameDictionaryResult>> HandleAsync(
        RenameDictionaryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<RenameDictionaryResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<RenameDictionaryResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        var dictionaryId = DictionaryId.From(command.DictionaryId);
        var dictionary = await _dictionaryRepository.GetAsync(dictionaryId, cancellationToken);
        if (dictionary is null)
        {
            return Result.Failure<RenameDictionaryResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        var owner = OwnerId.From(userId);

        try
        {
            var metadata = DictionaryMetadata.Create(
                command.Title,
                command.Description,
                command.Tags,
                command.Language,
                command.Region);
            dictionary.UpdateMetadata(owner, metadata);
        }
        catch (DomainException ex)
        {
            return Result.Failure<RenameDictionaryResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<RenameDictionaryResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<RenameDictionaryResult>(new UnexpectedError(
                "UnexpectedError",
                "An unexpected error occurred.",
                ex));
        }

        await _dictionaryRepository.UpdateAsync(dictionary, cancellationToken);
        await _eventPublisher.PublishAsync(dictionary.GetPendingEvents(), cancellationToken);
        dictionary.ClearPendingEvents();

        return Result.Success(new RenameDictionaryResult(command.DictionaryId, dictionary.Metadata.Title.Value));
    }
}

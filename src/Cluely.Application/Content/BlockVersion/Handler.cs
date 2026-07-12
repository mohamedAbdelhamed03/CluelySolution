using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.BlockVersion;

public sealed class BlockVersionHandler
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IContentModeratorAccessor _moderatorAccessor;
    private readonly IValidator<BlockVersionCommand> _validator;

    public BlockVersionHandler(
        IDictionaryRepository dictionaryRepository,
        IDomainEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor,
        IContentModeratorAccessor moderatorAccessor,
        IValidator<BlockVersionCommand> validator)
    {
        _dictionaryRepository = dictionaryRepository;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
        _moderatorAccessor = moderatorAccessor;
        _validator = validator;
    }

    public async Task<Result<BlockVersionResult>> HandleAsync(
        BlockVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<BlockVersionResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<BlockVersionResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        if (!_moderatorAccessor.IsModerator)
        {
            return Result.Failure<BlockVersionResult>(new BusinessError(
                "Forbidden",
                "Moderator authorization is required."));
        }

        var dictionaryId = DictionaryId.From(command.DictionaryId);
        var dictionary = await _dictionaryRepository.GetAsync(dictionaryId, cancellationToken);
        if (dictionary is null)
        {
            return Result.Failure<BlockVersionResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        var moderator = ModeratorId.From(userId);
        var versionId = VersionId.From(command.VersionId);

        try
        {
            dictionary.BlockVersion(moderator, versionId);
        }
        catch (DomainException ex)
        {
            return Result.Failure<BlockVersionResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<BlockVersionResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<BlockVersionResult>(new UnexpectedError(
                "UnexpectedError",
                "An unexpected error occurred.",
                ex));
        }

        await _dictionaryRepository.UpdateAsync(dictionary, cancellationToken);
        await _eventPublisher.PublishAsync(dictionary.GetPendingEvents(), cancellationToken);
        dictionary.ClearPendingEvents();

        return Result.Success(new BlockVersionResult(command.DictionaryId, command.VersionId));
    }
}

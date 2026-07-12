using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.UnblockVersion;

public sealed class UnblockVersionHandler
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IContentModeratorAccessor _moderatorAccessor;
    private readonly IValidator<UnblockVersionCommand> _validator;

    public UnblockVersionHandler(
        IDictionaryRepository dictionaryRepository,
        IDomainEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor,
        IContentModeratorAccessor moderatorAccessor,
        IValidator<UnblockVersionCommand> validator)
    {
        _dictionaryRepository = dictionaryRepository;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
        _moderatorAccessor = moderatorAccessor;
        _validator = validator;
    }

    public async Task<Result<UnblockVersionResult>> HandleAsync(
        UnblockVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<UnblockVersionResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<UnblockVersionResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        if (!_moderatorAccessor.IsModerator)
        {
            return Result.Failure<UnblockVersionResult>(new BusinessError(
                "Forbidden",
                "Moderator authorization is required."));
        }

        var dictionaryId = DictionaryId.From(command.DictionaryId);
        var dictionary = await _dictionaryRepository.GetAsync(dictionaryId, cancellationToken);
        if (dictionary is null)
        {
            return Result.Failure<UnblockVersionResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        var moderator = ModeratorId.From(userId);
        var versionId = VersionId.From(command.VersionId);

        try
        {
            dictionary.UnblockVersion(moderator, versionId);
        }
        catch (DomainException ex)
        {
            return Result.Failure<UnblockVersionResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<UnblockVersionResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<UnblockVersionResult>(new UnexpectedError(
                "UnexpectedError",
                "An unexpected error occurred.",
                ex));
        }

        await _dictionaryRepository.UpdateAsync(dictionary, cancellationToken);
        await _eventPublisher.PublishAsync(dictionary.GetPendingEvents(), cancellationToken);
        dictionary.ClearPendingEvents();

        return Result.Success(new UnblockVersionResult(command.DictionaryId, command.VersionId));
    }
}

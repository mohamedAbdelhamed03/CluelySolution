using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;
using FluentValidation;

namespace Cluely.Application.Content.RejectReview;

public sealed class RejectReviewHandler
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IContentModeratorAccessor _moderatorAccessor;
    private readonly IValidator<RejectReviewCommand> _validator;

    public RejectReviewHandler(
        IDictionaryRepository dictionaryRepository,
        IDomainEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor,
        IContentModeratorAccessor moderatorAccessor,
        IValidator<RejectReviewCommand> validator)
    {
        _dictionaryRepository = dictionaryRepository;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
        _moderatorAccessor = moderatorAccessor;
        _validator = validator;
    }

    public async Task<Result<RejectReviewResult>> HandleAsync(
        RejectReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<RejectReviewResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        if (_currentUserAccessor.UserId is not Guid userId)
        {
            return Result.Failure<RejectReviewResult>(new BusinessError(
                "Unauthorized",
                "Authentication is required."));
        }

        if (!_moderatorAccessor.IsModerator)
        {
            return Result.Failure<RejectReviewResult>(new BusinessError(
                "Forbidden",
                "Moderator authorization is required."));
        }

        var dictionaryId = DictionaryId.From(command.DictionaryId);
        var dictionary = await _dictionaryRepository.GetAsync(dictionaryId, cancellationToken);
        if (dictionary is null)
        {
            return Result.Failure<RejectReviewResult>(new BusinessError(
                "DictionaryNotFound",
                "Dictionary not found."));
        }

        var moderator = ModeratorId.From(userId);
        var versionId = VersionId.From(command.VersionId);

        try
        {
            dictionary.RejectReview(moderator, versionId);
        }
        catch (DomainException ex)
        {
            return Result.Failure<RejectReviewResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<RejectReviewResult>(new BusinessError(ex.GetType().Name, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<RejectReviewResult>(new UnexpectedError(
                "UnexpectedError",
                "An unexpected error occurred.",
                ex));
        }

        await _dictionaryRepository.UpdateAsync(dictionary, cancellationToken);
        await _eventPublisher.PublishAsync(dictionary.GetPendingEvents(), cancellationToken);
        dictionary.ClearPendingEvents();

        return Result.Success(new RejectReviewResult(command.DictionaryId, command.VersionId));
    }
}

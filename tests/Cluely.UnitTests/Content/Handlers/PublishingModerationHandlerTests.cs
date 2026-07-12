using Cluely.Application.Common.Results;
using Cluely.Application.Content.ApproveReview;
using Cluely.Application.Content.BlockVersion;
using Cluely.Application.Content.CancelDeleteDictionary;
using Cluely.Application.Content.PublishDictionary;
using Cluely.Application.Content.RejectReview;
using Cluely.Application.Content.ReportDictionary;
using Cluely.Application.Content.RetireVersion;
using Cluely.Application.Content.SubmitForReview;
using Cluely.Application.Content.UnblockVersion;
using Cluely.Domain.Content;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class PublishingModerationHandlerTests
{
    private readonly FakeDictionaryRepository _repository = new();
    private readonly FakeDomainEventPublisher _eventPublisher = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();
    private readonly FakeContentModeratorAccessor _moderatorAccessor = new();

    public PublishingModerationHandlerTests()
    {
        _currentUser.UserId = Guid.NewGuid();
        _moderatorAccessor.IsModerator = true;
    }

    [Fact]
    public async Task PublishHandler_Publishes_Valid_Draft()
    {
        var dictionary = SeedOwnedDictionaryWithWords(DictionaryValidation.MinWords);
        var versionId = Guid.NewGuid();
        var handler = new PublishDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new PublishDictionaryCommandValidator());

        var result = await handler.HandleAsync(new PublishDictionaryCommand(
            dictionary.Id.Value,
            versionId,
            Guid.NewGuid(),
            DateTime.UtcNow));

        result.IsSuccess.Should().BeTrue();
        result.Value!.WordCount.Should().Be(DictionaryValidation.MinWords);
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is VersionPublished);
    }

    [Fact]
    public async Task PublishHandler_On_Invalid_Draft_ShouldNotPersistOrPublishEvents()
    {
        var dictionary = SeedOwnedDictionaryWithWords(DictionaryValidation.MinWords - 1);
        var handler = new PublishDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new PublishDictionaryCommandValidator());

        var result = await handler.HandleAsync(new PublishDictionaryCommand(
            dictionary.Id.Value,
            Guid.NewGuid(),
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("DraftTooSmallException");
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishHandler_IdempotentReplay_ShouldNotPersistOrPublishEvents()
    {
        var dictionary = SeedOwnedDictionaryWithWords(DictionaryValidation.MinWords);
        var versionId = Guid.NewGuid();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        DictionaryTestData.ValidateAndPublish(dictionary, owner, VersionId.From(versionId), DateTime.UtcNow);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        _eventPublisher.PublishedEvents.Clear();
        _repository.ResetCounters();

        var handler = new PublishDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new PublishDictionaryCommandValidator());

        var result = await handler.HandleAsync(new PublishDictionaryCommand(
            dictionary.Id.Value,
            versionId,
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelDeleteHandler_Restores_Archived_State()
    {
        var dictionary = SeedOwnedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.Archive(owner);
        dictionary.RequestDeletion(owner);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new CancelDeleteDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new CancelDeleteDictionaryCommandValidator());

        var result = await handler.HandleAsync(new CancelDeleteDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.State.Should().Be(DictionaryState.Archived);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryDeletionCancelled);
    }

    [Fact]
    public async Task ReportHandler_Reports_Shared_Dictionary()
    {
        var dictionary = SeedOwnedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.SetVisibility(owner, Visibility.Shared);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new ReportDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ReportDictionaryCommandValidator());

        var result = await handler.HandleAsync(new ReportDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryReported);
    }

    [Fact]
    public async Task ReportHandler_On_Private_Dictionary_ShouldNotPersistOrPublishEvents()
    {
        var dictionary = SeedOwnedDictionary();
        var handler = new ReportDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ReportDictionaryCommandValidator());

        var result = await handler.HandleAsync(new ReportDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("VisibilityTransitionException");
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitForReviewHandler_Submits_Public_Version()
    {
        var dictionary = SeedPublishedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.SetVisibility(owner, Visibility.Public);
        var versionId = dictionary.CurrentVersionId!.Value;
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new SubmitForReviewHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new SubmitForReviewCommandValidator());

        var result = await handler.HandleAsync(new SubmitForReviewCommand(
            dictionary.Id.Value,
            versionId,
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is VersionSubmittedForReview);
    }

    [Fact]
    public async Task ApproveReviewHandler_Requires_Moderator()
    {
        var dictionary = SeedDictionaryPendingReview();
        _repository.Seed(dictionary);
        _moderatorAccessor.IsModerator = false;

        var handler = new ApproveReviewHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            _moderatorAccessor,
            new ApproveReviewCommandValidator());

        var result = await handler.HandleAsync(new ApproveReviewCommand(
            dictionary.Id.Value,
            dictionary.Versions.Single(version =>
                version.LifecycleState == VersionLifecycleState.PendingReview).Id.Value,
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("Forbidden");
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ApproveReviewHandler_Approves_Pending_Version()
    {
        var dictionary = SeedDictionaryPendingReview();
        _repository.Seed(dictionary);

        var handler = new ApproveReviewHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            _moderatorAccessor,
            new ApproveReviewCommandValidator());

        var versionId = dictionary.Versions.Single(version =>
            version.LifecycleState == VersionLifecycleState.PendingReview).Id.Value;

        var result = await handler.HandleAsync(new ApproveReviewCommand(
            dictionary.Id.Value,
            versionId,
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is ReviewApproved);
    }

    [Fact]
    public async Task RejectReviewHandler_OnFailure_ShouldNotPersistOrPublishEvents()
    {
        var dictionary = SeedPublishedDictionary();
        _repository.Seed(dictionary);

        var handler = new RejectReviewHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            _moderatorAccessor,
            new RejectReviewCommandValidator());

        var result = await handler.HandleAsync(new RejectReviewCommand(
            dictionary.Id.Value,
            dictionary.CurrentVersionId!.Value,
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task BlockAndUnblockHandlers_ShouldFollowReviewPath()
    {
        var dictionary = SeedPublishedDictionary();
        var versionId = dictionary.CurrentVersionId!.Value;
        _repository.Seed(dictionary);

        var blockHandler = new BlockVersionHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            _moderatorAccessor,
            new BlockVersionCommandValidator());

        var blockResult = await blockHandler.HandleAsync(new BlockVersionCommand(
            dictionary.Id.Value,
            versionId,
            Guid.NewGuid()));

        blockResult.IsSuccess.Should().BeTrue();
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is VersionBlocked);
        _eventPublisher.PublishedEvents.Clear();
        _repository.UpdateCount.Should().Be(1);

        var unblockHandler = new UnblockVersionHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            _moderatorAccessor,
            new UnblockVersionCommandValidator());

        var unblockResult = await unblockHandler.HandleAsync(new UnblockVersionCommand(
            dictionary.Id.Value,
            versionId,
            Guid.NewGuid()));

        unblockResult.IsSuccess.Should().BeTrue();
        dictionary.GetVersion(VersionId.From(versionId)).LifecycleState.Should().Be(VersionLifecycleState.PendingReview);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is VersionUnblocked);
    }

    [Fact]
    public async Task RetireVersionHandler_Allows_Owner_Or_Moderator()
    {
        var dictionary = SeedPublishedDictionary();
        var versionId = dictionary.CurrentVersionId!.Value;
        _repository.Seed(dictionary);
        _moderatorAccessor.IsModerator = false;

        var ownerHandler = new RetireVersionHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            _moderatorAccessor,
            new RetireVersionCommandValidator());

        var ownerResult = await ownerHandler.HandleAsync(new RetireVersionCommand(
            dictionary.Id.Value,
            versionId,
            Guid.NewGuid()));

        ownerResult.IsSuccess.Should().BeTrue();
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is VersionRetired);
    }

    [Fact]
    public async Task RetireVersionHandler_Forbids_NonOwner_NonModerator()
    {
        var dictionary = SeedPublishedDictionary();
        _repository.Seed(dictionary);
        _currentUser.UserId = Guid.NewGuid();
        _moderatorAccessor.IsModerator = false;

        var handler = new RetireVersionHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            _moderatorAccessor,
            new RetireVersionCommandValidator());

        var result = await handler.HandleAsync(new RetireVersionCommand(
            dictionary.Id.Value,
            dictionary.CurrentVersionId!.Value,
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("Forbidden");
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    private Dictionary SeedOwnedDictionary()
    {
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        var dictionary = Dictionary.Create(
            DictionaryId.From(Guid.NewGuid()),
            owner,
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return dictionary;
    }

    private Dictionary SeedOwnedDictionaryWithWords(int wordCount)
    {
        var dictionary = SeedOwnedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(wordCount));
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return dictionary;
    }

    private Dictionary SeedPublishedDictionary()
    {
        var dictionary = SeedOwnedDictionaryWithWords(DictionaryValidation.MinWords);
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return dictionary;
    }

    private Dictionary SeedDictionaryPendingReview()
    {
        var dictionary = SeedPublishedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.SetVisibility(owner, Visibility.Public);
        var versionId = dictionary.CurrentVersionId!;
        dictionary.SubmitVersionForReview(owner, versionId);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return dictionary;
    }
}

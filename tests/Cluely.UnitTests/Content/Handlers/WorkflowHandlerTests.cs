using Cluely.Application.Content.ApproveReview;
using Cluely.Application.Content.BlockVersion;
using Cluely.Application.Content.RejectReview;
using Cluely.Application.Content.RetireVersion;
using Cluely.Application.Content.SubmitForReview;
using Cluely.Application.Content.UnblockVersion;
using Cluely.Domain.Content;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

/// <summary>
/// Review and moderation workflow handlers (SubmitForReview, ApproveReview, RejectReview,
/// BlockVersion, UnblockVersion, RetireVersion). Submit/Retire are owner actions; Approve/Reject/
/// Block/Unblock require the moderator seam. All operations are lifecycle-only.
/// </summary>
public sealed class WorkflowHandlerTests
{
    private readonly FakeDictionaryRepository _repository = new();
    private readonly FakeDomainEventPublisher _eventPublisher = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();
    private readonly FakeContentModeratorAccessor _moderator = new();

    public WorkflowHandlerTests()
    {
        _currentUser.UserId = Guid.NewGuid();
        _moderator.IsModerator = true;
    }

    // --- SubmitForReview (owner) ---

    [Fact]
    public async Task SubmitForReview_ByOwner_ShouldMoveVersionToPendingReview()
    {
        var (dictionary, versionId) = SeedPublicPublishedVersion();

        var handler = new SubmitForReviewHandler(_repository, _eventPublisher, _currentUser, new SubmitForReviewCommandValidator());
        var result = await handler.HandleAsync(new SubmitForReviewCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.PendingReview);
        _eventPublisher.PublishedEvents.OfType<VersionSubmittedForReview>().Should().ContainSingle();
    }

    [Fact]
    public async Task SubmitForReview_ByNonOwner_ShouldFailWithoutPersisting()
    {
        var (dictionary, versionId) = SeedPublicPublishedVersion();
        _currentUser.UserId = Guid.NewGuid();

        var handler = new SubmitForReviewHandler(_repository, _eventPublisher, _currentUser, new SubmitForReviewCommandValidator());
        var result = await handler.HandleAsync(new SubmitForReviewCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    // --- ApproveReview (moderator) ---

    [Fact]
    public async Task ApproveReview_ByModerator_ShouldMakeVersionDiscoverable()
    {
        var (dictionary, versionId) = SeedPendingReviewVersion();

        var handler = new ApproveReviewHandler(_repository, _eventPublisher, _currentUser, _moderator, new ApproveReviewCommandValidator());
        var result = await handler.HandleAsync(new ApproveReviewCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Discoverable);
        _eventPublisher.PublishedEvents.OfType<ReviewApproved>().Should().ContainSingle();
    }

    [Fact]
    public async Task ApproveReview_ByNonModerator_ShouldBeForbiddenWithoutPersisting()
    {
        var (dictionary, versionId) = SeedPendingReviewVersion();
        _moderator.IsModerator = false;

        var handler = new ApproveReviewHandler(_repository, _eventPublisher, _currentUser, _moderator, new ApproveReviewCommandValidator());
        var result = await handler.HandleAsync(new ApproveReviewCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ApproveReview_Unauthenticated_ShouldFail()
    {
        var (dictionary, versionId) = SeedPendingReviewVersion();
        _currentUser.UserId = null;

        var handler = new ApproveReviewHandler(_repository, _eventPublisher, _currentUser, _moderator, new ApproveReviewCommandValidator());
        var result = await handler.HandleAsync(new ApproveReviewCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
    }

    // --- RejectReview (moderator) ---

    [Fact]
    public async Task RejectReview_ByModerator_ShouldReturnVersionToPublished()
    {
        var (dictionary, versionId) = SeedPendingReviewVersion();

        var handler = new RejectReviewHandler(_repository, _eventPublisher, _currentUser, _moderator, new RejectReviewCommandValidator());
        var result = await handler.HandleAsync(new RejectReviewCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Published);
        _eventPublisher.PublishedEvents.OfType<ReviewRejected>().Should().ContainSingle();
    }

    // --- BlockVersion / UnblockVersion (moderator) ---

    [Fact]
    public async Task BlockVersion_ByModerator_ShouldBlockAndClearCurrentPointer()
    {
        var (dictionary, versionId) = SeedPublicPublishedVersion();

        var handler = new BlockVersionHandler(_repository, _eventPublisher, _currentUser, _moderator, new BlockVersionCommandValidator());
        var result = await handler.HandleAsync(new BlockVersionCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Blocked);
        _eventPublisher.PublishedEvents.OfType<VersionBlocked>().Should().ContainSingle();
    }

    [Fact]
    public async Task UnblockVersion_ByModerator_ShouldReturnVersionToPendingReview()
    {
        var (dictionary, versionId) = SeedPublicPublishedVersion();
        dictionary.BlockVersion(ModeratorId.From(_currentUser.UserId!.Value), versionId);
        dictionary.ClearPendingEvents();

        var handler = new UnblockVersionHandler(_repository, _eventPublisher, _currentUser, _moderator, new UnblockVersionCommandValidator());
        var result = await handler.HandleAsync(new UnblockVersionCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.PendingReview);
        _eventPublisher.PublishedEvents.OfType<VersionUnblocked>().Should().ContainSingle();
    }

    // --- RetireVersion (moderator) ---

    [Fact]
    public async Task RetireVersion_ByModerator_ShouldRetireClearCurrentPointerAndEmitOneEvent()
    {
        var (dictionary, versionId) = SeedPublicPublishedVersion();

        var handler = new RetireVersionHandler(_repository, _eventPublisher, _currentUser, _moderator, new RetireVersionCommandValidator());
        var result = await handler.HandleAsync(new RetireVersionCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Retired);
        dictionary.CurrentVersionId.Should().BeNull();
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.OfType<VersionRetired>().Should().ContainSingle();
    }

    [Fact]
    public async Task RetireVersion_ByNonModerator_ShouldBeForbiddenWithoutPersisting()
    {
        var (dictionary, versionId) = SeedPublicPublishedVersion();
        _moderator.IsModerator = false;

        var handler = new RetireVersionHandler(_repository, _eventPublisher, _currentUser, _moderator, new RetireVersionCommandValidator());
        var result = await handler.HandleAsync(new RetireVersionCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task RetireVersion_ByOwnerWithoutModeratorPrivilege_ShouldBeForbidden()
    {
        // The current user owns the dictionary but is not a moderator: ownership must no longer suffice.
        var (dictionary, versionId) = SeedPublicPublishedVersion();
        _moderator.IsModerator = false;

        var handler = new RetireVersionHandler(_repository, _eventPublisher, _currentUser, _moderator, new RetireVersionCommandValidator());
        var result = await handler.HandleAsync(new RetireVersionCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Published);
    }

    [Fact]
    public async Task RetireVersion_Unauthenticated_ShouldFail()
    {
        var (dictionary, versionId) = SeedPublicPublishedVersion();
        _currentUser.UserId = null;

        var handler = new RetireVersionHandler(_repository, _eventPublisher, _currentUser, _moderator, new RetireVersionCommandValidator());
        var result = await handler.HandleAsync(new RetireVersionCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task RetireVersion_AlreadyRetired_ShouldFailWithoutPersistingOrPublishing()
    {
        var (dictionary, versionId) = SeedPublicPublishedVersion();
        dictionary.RetireVersion(ModeratorId.From(_currentUser.UserId!.Value), versionId);
        dictionary.ClearPendingEvents();

        var handler = new RetireVersionHandler(_repository, _eventPublisher, _currentUser, _moderator, new RetireVersionCommandValidator());
        var result = await handler.HandleAsync(new RetireVersionCommand(dictionary.Id.Value, versionId.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    // --- Missing dictionary (representative) ---

    [Fact]
    public async Task RetireVersion_MissingDictionary_ShouldFail()
    {
        var handler = new RetireVersionHandler(_repository, _eventPublisher, _currentUser, _moderator, new RetireVersionCommandValidator());
        var result = await handler.HandleAsync(new RetireVersionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DictionaryNotFound");
    }

    private (Dictionary dictionary, VersionId versionId) SeedPublicPublishedVersion()
    {
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        var dictionary = Dictionary.Create(
            DictionaryId.From(Guid.NewGuid()),
            owner,
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var versionId = VersionId.New();
        dictionary.Publish(owner, versionId, DateTime.UtcNow);
        dictionary.SetVisibility(owner, Visibility.Public);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return (dictionary, versionId);
    }

    private (Dictionary dictionary, VersionId versionId) SeedPendingReviewVersion()
    {
        var (dictionary, versionId) = SeedPublicPublishedVersion();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.SubmitVersionForReview(owner, versionId);
        dictionary.ClearPendingEvents();
        return (dictionary, versionId);
    }
}

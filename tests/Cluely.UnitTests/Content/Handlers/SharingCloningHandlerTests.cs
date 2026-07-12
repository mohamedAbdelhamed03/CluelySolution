using Cluely.Application.Content.CloneDictionary;
using Cluely.Application.Content.RevokeShare;
using Cluely.Application.Content.ShareDictionary;
using Cluely.Domain.Content;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class SharingCloningHandlerTests
{
    private readonly FakeDictionaryRepository _repository = new();
    private readonly FakeDomainEventPublisher _eventPublisher = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();
    private readonly FakeGuidGenerator _guidGenerator = new();
    private readonly Guid _userId = Guid.NewGuid();

    public SharingCloningHandlerTests()
    {
        _currentUser.UserId = _userId;
    }

    // --- ShareDictionary ---

    [Fact]
    public async Task Share_Success_ShouldRecordGrantAndPublishEvent()
    {
        var dictionary = SeedOwned(Visibility.Shared);
        var grantee = Guid.NewGuid();

        var result = await ShareHandler().HandleAsync(new ShareDictionaryCommand(dictionary.Id.Value, grantee, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.ShareGrants.Should().ContainSingle(g => g.GranteeId == OwnerId.From(grantee));
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.OfType<DictionaryShared>().Should().ContainSingle();
    }

    [Fact]
    public async Task Share_Duplicate_ShouldFailWithoutSecondPersist()
    {
        var dictionary = SeedOwned(Visibility.Shared);
        var grantee = Guid.NewGuid();
        await ShareHandler().HandleAsync(new ShareDictionaryCommand(dictionary.Id.Value, grantee, Guid.NewGuid()));
        _eventPublisher.PublishedEvents.Clear();

        var result = await ShareHandler().HandleAsync(new ShareDictionaryCommand(dictionary.Id.Value, grantee, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(nameof(Cluely.Domain.Content.Errors.DuplicateShareGrantException));
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Share_OnPrivateDictionary_ShouldFail()
    {
        var dictionary = SeedOwned(Visibility.Private);

        var result = await ShareHandler().HandleAsync(new ShareDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Share_ByNonOwner_ShouldFail()
    {
        var dictionary = SeedOwnedBy(Guid.NewGuid(), Visibility.Shared);

        var result = await ShareHandler().HandleAsync(new ShareDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(nameof(Cluely.Domain.Content.Errors.NotOwnerException));
        _repository.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Share_Unauthenticated_ShouldFail()
    {
        var dictionary = SeedOwned(Visibility.Shared);
        _currentUser.UserId = null;

        var result = await ShareHandler().HandleAsync(new ShareDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
    }

    // --- RevokeShare ---

    [Fact]
    public async Task Revoke_Success_ShouldRemoveGrantAndPublishEvent()
    {
        var owner = OwnerId.From(_userId);
        var dictionary = SeedOwned(Visibility.Shared);
        var grantee = OwnerId.From(Guid.NewGuid());
        dictionary.ShareWith(owner, grantee, DateTime.UtcNow);
        dictionary.ClearPendingEvents();

        var result = await RevokeHandler().HandleAsync(new RevokeShareCommand(dictionary.Id.Value, grantee.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.ShareGrants.Should().BeEmpty();
        _eventPublisher.PublishedEvents.OfType<ShareRevoked>().Should().ContainSingle();
    }

    [Fact]
    public async Task Revoke_UnknownGrantee_ShouldFailWithoutPersisting()
    {
        var dictionary = SeedOwned(Visibility.Shared);

        var result = await RevokeHandler().HandleAsync(new RevokeShareCommand(dictionary.Id.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(nameof(Cluely.Domain.Content.Errors.ShareGrantNotFoundException));
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    // --- CloneDictionary ---

    [Fact]
    public async Task Clone_AsOwner_ShouldCreateIndependentCloneWithProvenance()
    {
        var (source, versionId) = SeedOwnedPublished(Visibility.Private);
        var newId = Guid.NewGuid();
        _guidGenerator.Enqueue(newId);

        var result = await CloneHandler().HandleAsync(
            new CloneDictionaryCommand(source.Id.Value, versionId.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.DictionaryId.Should().Be(newId);
        _repository.AddCount.Should().Be(1);
        _repository.UpdateCount.Should().Be(0); // source is never modified

        var clone = await _repository.GetAsync(DictionaryId.From(newId));
        clone!.Owner.Should().Be(OwnerId.From(_userId));
        clone.Id.Should().NotBe(source.Id);
        clone.Provenance!.SourceVersionId.Should().Be(versionId);
        clone.Draft.Words.Count.Should().Be(25);
        _eventPublisher.PublishedEvents.OfType<DictionaryCloned>().Should().ContainSingle();
    }

    [Fact]
    public async Task Clone_NonOwnerOfPublicCurrentVersion_ShouldSucceed()
    {
        var (source, versionId) = SeedForeignPublished(Visibility.Public);
        _guidGenerator.Enqueue(Guid.NewGuid());

        var result = await CloneHandler().HandleAsync(
            new CloneDictionaryCommand(source.Id.Value, versionId.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        _repository.AddCount.Should().Be(1);
    }

    [Fact]
    public async Task Clone_NonOwnerOfNonCurrentVersion_ShouldReturnNotFound()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var source = Dictionary.Create(DictionaryId.From(Guid.NewGuid()), owner, ContentType.User, DictionaryTestData.DefaultMetadata());
        source.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var firstVersion = VersionId.New();
        DictionaryTestData.ValidateAndPublish(source, owner, firstVersion, DateTime.UtcNow);
        source.AddWords(owner, ["another"]);
        DictionaryTestData.ValidateAndPublish(source, owner, VersionId.New(), DateTime.UtcNow.AddMinutes(1));
        source.SetVisibility(owner, Visibility.Public);
        source.ClearPendingEvents();
        _repository.Seed(source);

        var result = await CloneHandler().HandleAsync(
            new CloneDictionaryCommand(source.Id.Value, firstVersion.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DictionaryNotFound");
        _repository.AddCount.Should().Be(0);
    }

    [Fact]
    public async Task Clone_OfPrivateDictionaryOfAnother_ShouldReturnNotFound()
    {
        var (source, versionId) = SeedForeignPublished(Visibility.Private);

        var result = await CloneHandler().HandleAsync(
            new CloneDictionaryCommand(source.Id.Value, versionId.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DictionaryNotFound");
    }

    [Fact]
    public async Task Clone_UnknownSource_ShouldReturnNotFound()
    {
        var result = await CloneHandler().HandleAsync(
            new CloneDictionaryCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DictionaryNotFound");
    }

    [Fact]
    public async Task Clone_DuplicateIdempotencyKey_ShouldReturnExistingWithoutAdding()
    {
        var (source, versionId) = SeedOwnedPublished(Visibility.Private);
        var idempotencyKey = Guid.NewGuid();
        var existingClone = Dictionary.Create(DictionaryId.From(Guid.NewGuid()), OwnerId.From(_userId), ContentType.User, DictionaryTestData.DefaultMetadata());
        _repository.SeedWithIdempotency(existingClone, idempotencyKey);

        var result = await CloneHandler().HandleAsync(
            new CloneDictionaryCommand(source.Id.Value, versionId.Value, Guid.NewGuid(), idempotencyKey));

        result.IsSuccess.Should().BeTrue();
        result.Value.DictionaryId.Should().Be(existingClone.Id.Value);
        _repository.AddCount.Should().Be(0);
    }

    [Fact]
    public async Task Clone_Unauthenticated_ShouldFail()
    {
        _currentUser.UserId = null;

        var result = await CloneHandler().HandleAsync(
            new CloneDictionaryCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
    }

    private ShareDictionaryHandler ShareHandler() =>
        new(_repository, _eventPublisher, _currentUser, new ShareDictionaryCommandValidator());

    private RevokeShareHandler RevokeHandler() =>
        new(_repository, _eventPublisher, _currentUser, new RevokeShareCommandValidator());

    private CloneDictionaryHandler CloneHandler() =>
        new(_repository, _eventPublisher, _currentUser, _guidGenerator, new CloneDictionaryCommandValidator());

    private Dictionary SeedOwned(Visibility visibility) => SeedOwnedBy(_userId, visibility);

    private Dictionary SeedOwnedBy(Guid ownerId, Visibility visibility)
    {
        var owner = OwnerId.From(ownerId);
        var dictionary = Dictionary.Create(DictionaryId.From(Guid.NewGuid()), owner, ContentType.User, DictionaryTestData.DefaultMetadata());
        if (visibility != Visibility.Private)
        {
            dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
            DictionaryTestData.ValidateAndPublish(dictionary, owner, VersionId.New(), DateTime.UtcNow);
            dictionary.SetVisibility(owner, visibility);
        }

        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return dictionary;
    }

    private (Dictionary Source, VersionId VersionId) SeedOwnedPublished(Visibility visibility) =>
        SeedPublishedBy(_userId, visibility);

    private (Dictionary Source, VersionId VersionId) SeedForeignPublished(Visibility visibility) =>
        SeedPublishedBy(Guid.NewGuid(), visibility);

    private (Dictionary Source, VersionId VersionId) SeedPublishedBy(Guid ownerId, Visibility visibility)
    {
        var owner = OwnerId.From(ownerId);
        var dictionary = Dictionary.Create(DictionaryId.From(Guid.NewGuid()), owner, ContentType.User, DictionaryTestData.DefaultMetadata());
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);
        if (visibility != Visibility.Private)
        {
            dictionary.SetVisibility(owner, visibility);
        }

        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return (dictionary, versionId);
    }
}

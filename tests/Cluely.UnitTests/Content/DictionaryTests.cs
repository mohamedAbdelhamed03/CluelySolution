using Cluely.Domain.Content;
using Cluely.Domain.Content.Entities;
using Cluely.Domain.Content.Errors;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

public sealed class DictionaryTests
{
    [Fact]
    public void Create_ShouldAssignOwnerMetadataAndPrivateVisibility()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = Dictionary.Create(
            DictionaryId.New(),
            owner,
            ContentType.User,
            DictionaryTestData.DefaultMetadata());

        dictionary.Owner.Should().Be(owner);
        dictionary.Visibility.Should().Be(Visibility.Private);
        dictionary.State.Should().Be(DictionaryState.Active);
        dictionary.ContentType.Should().Be(ContentType.User);
        dictionary.Draft.Words.Count.Should().Be(0);
        dictionary.Draft.State.Should().Be(DraftState.Draft);
        dictionary.Versions.Should().BeEmpty();
        dictionary.CurrentVersionId.Should().BeNull();
        dictionary.GetPendingEvents().OfType<DictionaryCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Create_ShouldExposeExactlyOneDraft()
    {
        var dictionary = CreateDictionary();

        dictionary.Draft.Should().NotBeNull();
        dictionary.Draft.State.Should().Be(DraftState.Draft);
    }

    [Fact]
    public void UpdateMetadata_ByNonOwner_ShouldThrow()
    {
        var dictionary = CreateDictionary();
        var metadata = DictionaryMetadata.Create("Renamed", "desc", [], "en");

        Action action = () => dictionary.UpdateMetadata(OwnerId.From(Guid.NewGuid()), metadata);

        action.Should().Throw<NotOwnerException>();
    }

    [Fact]
    public void UpdateMetadata_ByOwner_ShouldRaiseDictionaryRenamed()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = Dictionary.Create(
            DictionaryId.New(),
            owner,
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
        dictionary.ClearPendingEvents();

        var metadata = DictionaryMetadata.Create("Renamed Dictionary", "updated", ["fun"], "en");
        dictionary.UpdateMetadata(owner, metadata);

        dictionary.Metadata.Title.Value.Should().Be("Renamed Dictionary");
        dictionary.GetPendingEvents().OfType<DictionaryRenamed>().Should().ContainSingle();
        dictionary.Version.Value.Should().Be(1);
    }

    [Fact]
    public void AddWords_ShouldNormalizeAndDeduplicate()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.ClearPendingEvents();

        dictionary.AddWords(owner, ["  Alpha  ", "alpha", "Beta"]);

        dictionary.Draft.Words.Count.Should().Be(2);
        dictionary.Draft.Words.Words.Select(word => word.Value).Should().BeEquivalentTo(["alpha", "beta"]);
        dictionary.GetPendingEvents().OfType<WordsChanged>().Should().ContainSingle();
    }

    [Fact]
    public void AddWords_DuplicateAfterNormalization_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, ["Alpha"]);

        Action action = () => dictionary.AddWords(owner, ["  alpha "]);

        action.Should().Throw<DuplicateWordException>();
    }

    [Fact]
    public void Publish_WithoutPreValidation_ShouldValidateAndPublish()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));

        dictionary.Publish(owner, VersionId.New(), DateTime.UtcNow);

        dictionary.Versions.Should().ContainSingle();
    }

    [Fact]
    public void Publish_WithTooFewWords_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(24));

        Action action = () => dictionary.Publish(owner, VersionId.New(), DateTime.UtcNow);

        action.Should().Throw<DraftTooSmallException>();
    }

    [Fact]
    public void Publish_WithValidDraft_ShouldCreateImmutableVersionAndKeepDraft()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var versionId = VersionId.New();
        var publishedAt = DateTime.UtcNow;

        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, publishedAt);

        dictionary.Versions.Should().ContainSingle();
        var version = dictionary.GetVersion(versionId);
        version.Words.Count.Should().Be(25);
        version.Label.Value.Should().Be(1);
        version.LifecycleState.Should().Be(VersionLifecycleState.Published);
        version.PublishedAt.Should().Be(publishedAt);
        dictionary.CurrentVersionId.Should().Be(versionId);
        dictionary.Draft.Words.Count.Should().Be(25);
        dictionary.Draft.State.Should().Be(DraftState.Draft);
        dictionary.GetPendingEvents().OfType<VersionPublished>().Should().ContainSingle();
    }

    [Fact]
    public void PublishedVersion_ShouldNotExposeMutableOperations()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);

        var publicMethods = typeof(DictionaryVersion)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToList();

        publicMethods.Should().BeEmpty();
    }

    [Fact]
    public void Publish_SecondVersion_ShouldDeprecatePreviousCurrent()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var firstVersionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, firstVersionId, DateTime.UtcNow);

        dictionary.AddWords(owner, ["extra"]);
        var secondVersionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, secondVersionId, DateTime.UtcNow.AddMinutes(1));

        dictionary.GetVersion(firstVersionId).LifecycleState.Should().Be(VersionLifecycleState.Deprecated);
        dictionary.GetVersion(secondVersionId).LifecycleState.Should().Be(VersionLifecycleState.Published);
        dictionary.CurrentVersionId.Should().Be(secondVersionId);
    }

    [Fact]
    public void DiscardDraft_ShouldRevertToLastPublishedWords()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        DictionaryTestData.ValidateAndPublish(dictionary, owner, VersionId.New(), DateTime.UtcNow);
        dictionary.ClearPendingEvents();

        dictionary.AddWords(owner, ["temporary"]);
        dictionary.DiscardDraft(owner);

        dictionary.Draft.Words.Count.Should().Be(25);
        dictionary.GetPendingEvents().OfType<DraftDiscarded>().Should().ContainSingle();
    }

    [Fact]
    public void SetVisibilityToPublic_WithoutPublishedVersion_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);

        Action action = () => dictionary.SetVisibility(owner, Visibility.Public);

        action.Should().Throw<VisibilityTransitionException>();
    }

    [Fact]
    public void PublicPublish_ShouldStartInPendingReview()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        DictionaryTestData.ValidateAndPublish(dictionary, owner, VersionId.New(), DateTime.UtcNow);
        dictionary.SetVisibility(owner, Visibility.Public);
        dictionary.ClearPendingEvents();

        dictionary.AddWords(owner, ["another"]);
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow.AddMinutes(1));

        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.PendingReview);
        dictionary.CurrentVersionId.Should().BeNull();
    }

    [Fact]
    public void ReviewApproval_ShouldMakeVersionDiscoverable()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);
        dictionary.SetVisibility(owner, Visibility.Public);
        dictionary.SubmitVersionForReview(owner, versionId);

        dictionary.ApproveReview(ModeratorId.From(Guid.NewGuid()), versionId);

        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Discoverable);
        dictionary.GetPendingEvents().OfType<ReviewApproved>().Should().ContainSingle();
    }

    [Fact]
    public void BlockAndUnblock_ShouldRequireReviewBeforeBecomingCurrent()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);

        dictionary.BlockVersion(ModeratorId.From(Guid.NewGuid()), versionId);
        dictionary.CurrentVersionId.Should().BeNull();

        dictionary.UnblockVersion(ModeratorId.From(Guid.NewGuid()), versionId);
        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.PendingReview);
        dictionary.CurrentVersionId.Should().BeNull();

        dictionary.ApproveReview(ModeratorId.From(Guid.NewGuid()), versionId);
        dictionary.CurrentVersionId.Should().Be(versionId);
    }

    [Fact]
    public void RetireVersion_ShouldRemoveItFromCurrentPointer()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);

        dictionary.RetireVersion(owner, versionId);

        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Retired);
        dictionary.CurrentVersionId.Should().BeNull();
    }

    [Fact]
    public void ShareWith_WhenPrivate_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        var grantee = OwnerId.From(Guid.NewGuid());

        Action action = () => dictionary.ShareWith(owner, grantee, DateTime.UtcNow);

        action.Should().Throw<VisibilityTransitionException>();
    }

    [Fact]
    public void ShareWith_WhenShared_ShouldRecordGrant()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.SetVisibility(owner, Visibility.Shared);
        var grantee = OwnerId.From(Guid.NewGuid());

        dictionary.ShareWith(owner, grantee, DateTime.UtcNow);

        dictionary.ShareGrants.Should().ContainSingle(grant => grant.GranteeId == grantee);
        dictionary.GetPendingEvents().OfType<DictionaryShared>().Should().ContainSingle();
    }

    [Fact]
    public void CloneFrom_ShouldCreateIndependentOwnedDictionary()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var source = CreateDictionary(owner);
        source.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var sourceVersionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(source, owner, sourceVersionId, DateTime.UtcNow);
        var sourceVersion = source.GetVersion(sourceVersionId);

        var cloner = OwnerId.From(Guid.NewGuid());
        var clone = Dictionary.CloneFrom(
            DictionaryId.New(),
            cloner,
            source,
            sourceVersion,
            DictionaryMetadata.Create("Clone", "cloned", [], "en"));

        clone.Owner.Should().Be(cloner);
        clone.Provenance!.SourceVersionId.Should().Be(sourceVersionId);
        clone.Draft.Words.Count.Should().Be(25);
        clone.Id.Should().NotBe(source.Id);
        clone.GetPendingEvents().OfType<DictionaryCloned>().Should().ContainSingle();
    }

    [Fact]
    public void ArchiveRestoreAndDeletion_ShouldFollowContainerLifecycle()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);

        dictionary.Archive(owner);
        dictionary.State.Should().Be(DictionaryState.Archived);

        dictionary.Restore(owner);
        dictionary.State.Should().Be(DictionaryState.Active);

        dictionary.RequestDeletion(owner);
        dictionary.State.Should().Be(DictionaryState.PendingDeletion);

        dictionary.CancelDeletion(owner);
        dictionary.State.Should().Be(DictionaryState.Active);

        dictionary.RequestDeletion(owner);
        dictionary.CompleteDeletion(owner);
        dictionary.State.Should().Be(DictionaryState.Deleted);
    }

    [Fact]
    public void Authoring_OnArchivedDictionary_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.Archive(owner);

        Action action = () => dictionary.AddWords(owner, ["blocked"]);

        action.Should().Throw<DictionaryLifecycleException>();
    }

    [Fact]
    public void DictionaryIds_WithSameValue_ShouldBeEqual()
    {
        var value = Guid.NewGuid();
        var first = DictionaryId.From(value);
        var second = DictionaryId.From(value);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    private static Dictionary CreateDictionary(OwnerId? owner = null)
    {
        return Dictionary.Create(
            DictionaryId.New(),
            owner ?? OwnerId.From(Guid.NewGuid()),
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
    }
}

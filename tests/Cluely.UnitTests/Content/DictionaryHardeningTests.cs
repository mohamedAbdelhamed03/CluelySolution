using Cluely.Domain.Content;
using Cluely.Domain.Content.Errors;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

/// <summary>
/// Additional coverage added during the Slices 00–02 engineering hardening review: clone isolation,
/// invalid lifecycle transitions, ownership enforcement across operations, and event emission that the
/// existing suite did not assert. All scenarios assert the aggregate's intended behavior.
/// </summary>
public sealed class DictionaryHardeningTests
{
    // --- Clone isolation (AI-CP-8 / FF-CP-008) ---

    [Fact]
    public void CloneFrom_EditingCloneOrSource_ShouldNotAffectTheOther()
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

        clone.AddWords(cloner, ["cloneonly"]);
        source.AddWords(owner, ["sourceonly"]);

        clone.Draft.Words.Words.Select(word => word.Value).Should().Contain("cloneonly");
        clone.Draft.Words.Words.Select(word => word.Value).Should().NotContain("sourceonly");
        source.Draft.Words.Words.Select(word => word.Value).Should().Contain("sourceonly");
        source.Draft.Words.Words.Select(word => word.Value).Should().NotContain("cloneonly");

        // The immutable source version is untouched by either draft edit.
        source.GetVersion(sourceVersionId).Words.Count.Should().Be(25);
    }

    [Fact]
    public void CloneFrom_VersionOfDifferentDictionary_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var source = CreateDictionary(owner);
        source.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        DictionaryTestData.ValidateAndPublish(source, owner, VersionId.New(), DateTime.UtcNow);

        var otherOwner = OwnerId.From(Guid.NewGuid());
        var other = CreateDictionary(otherOwner);
        other.AddWords(otherOwner, DictionaryTestData.ValidWordBatch(25));
        var otherVersionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(other, otherOwner, otherVersionId, DateTime.UtcNow);
        var foreignVersion = other.GetVersion(otherVersionId);

        Action action = () => Dictionary.CloneFrom(
            DictionaryId.New(),
            OwnerId.From(Guid.NewGuid()),
            source,
            foreignVersion,
            DictionaryMetadata.Create("Clone", "cloned", [], "en"));

        action.Should().Throw<VersionNotFoundException>();
    }

    // --- Invalid lifecycle transitions ---

    [Fact]
    public void Restore_WhenActive_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);

        Action action = () => dictionary.Restore(owner);

        action.Should().Throw<DictionaryLifecycleException>();
    }

    [Fact]
    public void CancelDeletion_WhenNotPending_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);

        Action action = () => dictionary.CancelDeletion(owner);

        action.Should().Throw<DictionaryLifecycleException>();
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.Archive(owner);

        Action action = () => dictionary.Archive(owner);

        action.Should().Throw<DictionaryLifecycleException>();
    }

    [Fact]
    public void RetireVersion_WhenAlreadyRetired_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);
        dictionary.RetireVersion(owner, versionId);

        Action action = () => dictionary.RetireVersion(owner, versionId);

        action.Should().Throw<VersionLifecycleException>();
    }

    [Fact]
    public void ApproveReview_WhenNotPendingReview_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);

        Action action = () => dictionary.ApproveReview(ModeratorId.From(Guid.NewGuid()), versionId);

        action.Should().Throw<VersionLifecycleException>();
    }

    // --- Ownership enforcement across operations ---

    [Fact]
    public void Archive_ByNonOwner_ShouldThrow()
    {
        var dictionary = CreateDictionary();

        Action action = () => dictionary.Archive(OwnerId.From(Guid.NewGuid()));

        action.Should().Throw<NotOwnerException>();
    }

    [Fact]
    public void Publish_ByNonOwner_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));

        Action action = () => dictionary.Publish(OwnerId.From(Guid.NewGuid()), VersionId.New(), DateTime.UtcNow);

        action.Should().Throw<NotOwnerException>();
    }

    // --- Event emission not previously asserted ---

    [Fact]
    public void SetVisibility_ShouldRaiseVisibilityChanged()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.ClearPendingEvents();

        dictionary.SetVisibility(owner, Visibility.Shared);

        dictionary.Visibility.Should().Be(Visibility.Shared);
        dictionary.GetPendingEvents().OfType<VisibilityChanged>().Should().ContainSingle();
    }

    [Fact]
    public void Archive_ShouldRaiseDictionaryArchived()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.ClearPendingEvents();

        dictionary.Archive(owner);

        dictionary.GetPendingEvents().OfType<DictionaryArchived>().Should().ContainSingle();
    }

    // --- Version label ordering ---

    [Fact]
    public void Publish_SecondVersion_ShouldIncrementLabel()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        var first = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, first, DateTime.UtcNow);

        dictionary.AddWords(owner, ["extra"]);
        var second = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, second, DateTime.UtcNow.AddMinutes(1));

        dictionary.GetVersion(first).Label.Value.Should().Be(1);
        dictionary.GetVersion(second).Label.Value.Should().Be(2);
    }

    // --- Discard with no published version ---

    [Fact]
    public void DiscardDraft_WithNoPublishedVersion_ShouldRevertToEmpty()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, ["temp1", "temp2"]);

        dictionary.DiscardDraft(owner);

        dictionary.Draft.Words.Count.Should().Be(0);
    }

    [Fact]
    public void AddWords_WhenDuplicateFails_ShouldNotChangeStateVersionOrEvents()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, ["alpha"]);
        dictionary.ClearPendingEvents();
        var versionBeforeFailure = dictionary.Version;

        Action action = () => dictionary.AddWords(owner, ["beta", " ALPHA "]);

        action.Should().Throw<DuplicateWordException>();
        dictionary.Draft.Words.Words.Select(word => word.Value).Should().Equal("alpha");
        dictionary.Version.Should().Be(versionBeforeFailure);
        dictionary.GetPendingEvents().Should().BeEmpty();
    }

    [Fact]
    public void Publish_WhenDraftIsInvalid_ShouldNotAdvanceVersionLabelOrRaiseEvent()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(DictionaryValidation.MinWords - 1));
        dictionary.ClearPendingEvents();
        var aggregateVersionBeforeFailure = dictionary.Version;
        var nextLabelBeforeFailure = dictionary.NextVersionLabel;

        Action action = () => dictionary.Publish(owner, VersionId.New(), DateTime.UtcNow);

        action.Should().Throw<DraftTooSmallException>();
        dictionary.Versions.Should().BeEmpty();
        dictionary.CurrentVersionId.Should().BeNull();
        dictionary.NextVersionLabel.Should().Be(nextLabelBeforeFailure);
        dictionary.Version.Should().Be(aggregateVersionBeforeFailure);
        dictionary.GetPendingEvents().Should().BeEmpty();
    }

    [Fact]
    public void CloneFrom_ShouldRaiseCreatedBeforeCloned()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var source = CreateDictionary(owner);
        source.AddWords(owner, DictionaryTestData.ValidWordBatch(DictionaryValidation.MinWords));
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(source, owner, versionId, DateTime.UtcNow);

        var clone = Dictionary.CloneFrom(
            DictionaryId.New(),
            OwnerId.From(Guid.NewGuid()),
            source,
            source.GetVersion(versionId),
            DictionaryTestData.DefaultMetadata());

        clone.GetPendingEvents().Select(domainEvent => domainEvent.GetType()).Should().Equal(
            typeof(DictionaryCreated),
            typeof(DictionaryCloned));
    }

    [Fact]
    public void RejectReview_ShouldRestorePublishedVersionAsCurrent()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(DictionaryValidation.MinWords));
        dictionary.SetVisibility(owner, Visibility.Shared);
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);
        dictionary.SetVisibility(owner, Visibility.Public);
        dictionary.SubmitVersionForReview(owner, versionId);
        dictionary.ClearPendingEvents();

        dictionary.RejectReview(ModeratorId.From(Guid.NewGuid()), versionId);

        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Published);
        dictionary.CurrentVersionId.Should().Be(versionId);
        dictionary.GetPendingEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ReviewRejected>();
    }

    [Fact]
    public void CancelDeletion_FromArchived_ShouldRestoreArchived()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.Archive(owner);
        dictionary.RequestDeletion(owner);

        dictionary.CancelDeletion(owner);

        dictionary.State.Should().Be(DictionaryState.Archived);
    }

    [Fact]
    public void ApproveReview_RequiresModeratorPrincipal_NotOwner()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(DictionaryValidation.MinWords));
        var versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);
        dictionary.SetVisibility(owner, Visibility.Public);
        dictionary.SubmitVersionForReview(owner, versionId);

        var moderator = ModeratorId.From(Guid.NewGuid());
        dictionary.ApproveReview(moderator, versionId);

        dictionary.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Discoverable);
    }

    [Fact]
    public void Report_OnPrivateDictionary_ShouldThrow()
    {
        var reporter = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary();

        Action action = () => dictionary.Report(reporter);

        action.Should().Throw<VisibilityTransitionException>();
    }

    [Fact]
    public void Report_DuplicateReports_ShouldRaiseSeparateEvents()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.SetVisibility(owner, Visibility.Shared);
        dictionary.ClearPendingEvents();
        var reporter = OwnerId.From(Guid.NewGuid());

        dictionary.Report(reporter);
        dictionary.ClearPendingEvents();
        dictionary.Report(reporter);

        dictionary.GetPendingEvents().OfType<DictionaryReported>().Should().ContainSingle();
    }

    [Fact]
    public void Report_OnSharedDictionary_ShouldRaiseDictionaryReported()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.SetVisibility(owner, Visibility.Shared);
        dictionary.ClearPendingEvents();
        var reporter = OwnerId.From(Guid.NewGuid());

        dictionary.Report(reporter);

        dictionary.GetPendingEvents().OfType<DictionaryReported>().Should().ContainSingle()
            .Which.ReporterId.Should().Be(reporter);
    }

    [Fact]
    public void ValidateDraft_ShouldNotAdvanceAggregateVersion()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(DictionaryValidation.MinWords));
        dictionary.ClearPendingEvents();
        var aggregateVersionBeforeValidation = dictionary.Version;

        dictionary.ValidateDraft(owner);

        dictionary.Version.Should().Be(aggregateVersionBeforeValidation);
        dictionary.GetPendingEvents().Should().BeEmpty();
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

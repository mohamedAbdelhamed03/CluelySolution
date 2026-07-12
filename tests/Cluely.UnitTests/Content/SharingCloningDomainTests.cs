using Cluely.Domain.Content;
using Cluely.Domain.Content.Errors;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

public sealed class SharingCloningDomainTests
{
    // --- TD-001: ShareGrant equality by grantee only ---

    [Fact]
    public void ShareWith_SameGranteeTwice_ShouldThrowAndKeepSingleGrant()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateShared(owner);
        var grantee = OwnerId.From(Guid.NewGuid());
        dictionary.ShareWith(owner, grantee, DateTime.UtcNow);

        // A later timestamp must not defeat duplicate detection (TD-001).
        Action second = () => dictionary.ShareWith(owner, grantee, DateTime.UtcNow.AddMinutes(5));

        second.Should().Throw<DuplicateShareGrantException>();
        dictionary.ShareGrants.Should().ContainSingle(grant => grant.GranteeId == grantee);
    }

    [Fact]
    public void ShareGrants_WithSameGrantee_AreEqualRegardlessOfTimestamp()
    {
        var grantee = OwnerId.From(Guid.NewGuid());
        var first = ShareGrant.Create(grantee, DateTime.UtcNow);
        var second = ShareGrant.Create(grantee, DateTime.UtcNow.AddHours(1));

        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void RevokeShare_AfterDuplicateAttempt_ShouldRemainDeterministic()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateShared(owner);
        var grantee = OwnerId.From(Guid.NewGuid());
        dictionary.ShareWith(owner, grantee, DateTime.UtcNow);

        dictionary.RevokeShare(owner, grantee);

        dictionary.ShareGrants.Should().BeEmpty();
    }

    [Fact]
    public void RevokeShare_UnknownGrantee_ShouldThrow()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateShared(owner);

        Action action = () => dictionary.RevokeShare(owner, OwnerId.From(Guid.NewGuid()));

        action.Should().Throw<ShareGrantNotFoundException>();
    }

    [Fact]
    public void ShareWith_ByNonOwner_ShouldThrow_AndPreserveOwner()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateShared(owner);

        Action action = () => dictionary.ShareWith(OwnerId.From(Guid.NewGuid()), OwnerId.From(Guid.NewGuid()), DateTime.UtcNow);

        action.Should().Throw<NotOwnerException>();
        dictionary.Owner.Should().Be(owner);
    }

    // --- IsViewableBy ---

    [Fact]
    public void IsViewableBy_ShouldFollowVisibilityRules()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var grantee = OwnerId.From(Guid.NewGuid());
        var stranger = OwnerId.From(Guid.NewGuid());

        var privateDict = CreateDictionary(owner);
        privateDict.IsViewableBy(owner).Should().BeTrue();
        privateDict.IsViewableBy(stranger).Should().BeFalse();

        var sharedDict = CreateShared(owner);
        sharedDict.ShareWith(owner, grantee, DateTime.UtcNow);
        sharedDict.IsViewableBy(grantee).Should().BeTrue();
        sharedDict.IsViewableBy(stranger).Should().BeFalse();

        var publicDict = CreatePublished(owner);
        publicDict.SetVisibility(owner, Visibility.Public);
        publicDict.IsViewableBy(stranger).Should().BeTrue();
    }

    // --- Clone provenance & independence ---

    [Fact]
    public void CloneFrom_ShouldRecordFullImmutableProvenance()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var source = CreatePublished(owner, out var sourceVersionId);
        var clonedAt = new DateTime(2026, 07, 12, 10, 30, 00, DateTimeKind.Utc);

        var clone = Dictionary.CloneFrom(
            DictionaryId.New(),
            OwnerId.From(Guid.NewGuid()),
            source,
            sourceVersionId,
            DictionaryTestData.DefaultMetadata(),
            clonedAt);

        clone.Provenance!.SourceDictionaryId.Should().Be(source.Id);
        clone.Provenance!.SourceVersionId.Should().Be(sourceVersionId);
        clone.Provenance!.OriginType.Should().Be(OriginType.Clone);
        clone.Provenance!.ClonedAt.Should().Be(clonedAt);
    }

    [Fact]
    public void CloneFrom_ShouldNotMutateSource()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var source = CreatePublished(owner, out var sourceVersionId);
        var sourceVersionBefore = source.Version.Value;
        source.ClearPendingEvents();

        var clone = Dictionary.CloneFrom(
            DictionaryId.New(),
            OwnerId.From(Guid.NewGuid()),
            source,
            sourceVersionId,
            DictionaryTestData.DefaultMetadata(),
            DateTime.UtcNow);

        source.Version.Value.Should().Be(sourceVersionBefore);
        source.GetPendingEvents().Should().BeEmpty();
        source.GetVersion(sourceVersionId).Words.Count.Should().Be(25);
        clone.Id.Should().NotBe(source.Id);
        clone.Version.Value.Should().Be(0);
    }

    private static Dictionary CreateDictionary(OwnerId owner) =>
        Dictionary.Create(DictionaryId.New(), owner, ContentType.User, DictionaryTestData.DefaultMetadata());

    private static Dictionary CreateShared(OwnerId owner)
    {
        var dictionary = CreateDictionary(owner);
        dictionary.SetVisibility(owner, Visibility.Shared);
        return dictionary;
    }

    private static Dictionary CreatePublished(OwnerId owner) => CreatePublished(owner, out _);

    private static Dictionary CreatePublished(OwnerId owner, out VersionId versionId)
    {
        var dictionary = CreateDictionary(owner);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        versionId = VersionId.New();
        DictionaryTestData.ValidateAndPublish(dictionary, owner, versionId, DateTime.UtcNow);
        return dictionary;
    }
}

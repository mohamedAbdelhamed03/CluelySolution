using Cluely.Domain.Content;
using Cluely.Domain.Content.Errors;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

/// <summary>
/// Publishing-specific domain guarantees for Slice 04B: a published version is an immutable snapshot,
/// publishing never edits history, and a failed publish leaves no trace.
/// </summary>
public sealed class PublishingDomainTests
{
    [Fact]
    public void Publish_ThenEditDraft_ShouldNotChangePublishedVersion()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateWithWords(owner, 25);
        var versionId = VersionId.New();
        dictionary.Publish(owner, versionId, DateTime.UtcNow);

        dictionary.AddWords(owner, ["afterpublish"]);

        dictionary.GetVersion(versionId).Words.Count.Should().Be(25);
        dictionary.Draft.Words.Count.Should().Be(26);
    }

    [Fact]
    public void Publish_SecondVersion_ShouldNotChangeFirstVersionWords()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateWithWords(owner, 25);
        var first = VersionId.New();
        dictionary.Publish(owner, first, DateTime.UtcNow);

        dictionary.AddWords(owner, ["extra"]);
        var second = VersionId.New();
        dictionary.Publish(owner, second, DateTime.UtcNow.AddMinutes(1));

        dictionary.GetVersion(first).Words.Count.Should().Be(25);
        dictionary.GetVersion(second).Words.Count.Should().Be(26);
    }

    [Fact]
    public void Publish_ShouldIncrementAggregateVersionExactlyOnce()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateWithWords(owner, 25);
        var before = dictionary.Version.Value;

        dictionary.Publish(owner, VersionId.New(), DateTime.UtcNow);

        dictionary.Version.Value.Should().Be(before + 1);
    }

    [Fact]
    public void Publish_ShouldRaiseExactlyOneVersionPublishedEvent()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateWithWords(owner, 25);
        dictionary.ClearPendingEvents();

        dictionary.Publish(owner, VersionId.New(), DateTime.UtcNow);

        dictionary.GetPendingEvents().OfType<VersionPublished>().Should().ContainSingle();
    }

    [Fact]
    public void Publish_Failure_ShouldNotAddVersionEventOrIncrementAggregateVersion()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = CreateWithWords(owner, 24);
        var before = dictionary.Version.Value;

        Action action = () => dictionary.Publish(owner, VersionId.New(), DateTime.UtcNow);

        action.Should().Throw<DraftTooSmallException>();
        dictionary.Versions.Should().BeEmpty();
        dictionary.CurrentVersionId.Should().BeNull();
        dictionary.GetPendingEvents().OfType<VersionPublished>().Should().BeEmpty();
        dictionary.Version.Value.Should().Be(before);
    }

    private static Dictionary CreateWithWords(OwnerId owner, int wordCount)
    {
        var dictionary = Dictionary.Create(
            DictionaryId.New(),
            owner,
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(wordCount));
        return dictionary;
    }
}

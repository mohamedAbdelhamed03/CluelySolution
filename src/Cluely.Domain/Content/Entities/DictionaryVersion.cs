using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Content.Entities;

public sealed class DictionaryVersion
{
    public VersionId Id { get; }
    public DictionaryId DictionaryId { get; }
    public VersionLabel Label { get; }
    public WordSet Words { get; }
    public VersionLifecycleState LifecycleState { get; private set; }
    public DateTime PublishedAt { get; }

    private DictionaryVersion(
        VersionId id,
        DictionaryId dictionaryId,
        VersionLabel label,
        WordSet words,
        VersionLifecycleState lifecycleState,
        DateTime publishedAt)
    {
        Id = id;
        DictionaryId = dictionaryId;
        Label = label;
        Words = words;
        LifecycleState = lifecycleState;
        PublishedAt = publishedAt;
    }

    internal static DictionaryVersion Publish(
        VersionId id,
        DictionaryId dictionaryId,
        VersionLabel label,
        WordSet words,
        VersionLifecycleState initialState,
        DateTime publishedAt)
    {
        return new DictionaryVersion(id, dictionaryId, label, words.Copy(), initialState, publishedAt);
    }

    internal static DictionaryVersion Rehydrate(
        VersionId id,
        DictionaryId dictionaryId,
        VersionLabel label,
        WordSet words,
        VersionLifecycleState lifecycleState,
        DateTime publishedAt)
    {
        return new DictionaryVersion(id, dictionaryId, label, words, lifecycleState, publishedAt);
    }

    internal void TransitionTo(VersionLifecycleState nextState)
    {
        LifecycleState = nextState;
    }

    internal bool CanServeAsCurrent()
    {
        return LifecycleState is VersionLifecycleState.Published or VersionLifecycleState.Discoverable;
    }
}

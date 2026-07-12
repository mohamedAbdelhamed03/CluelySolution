using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Content.Entities;

public sealed class DictionaryDraft
{
    public WordSet Words { get; private set; }
    public DraftState State { get; private set; }

    private DictionaryDraft(WordSet words, DraftState state)
    {
        Words = words;
        State = state;
    }

    public static DictionaryDraft Empty() => new(WordSet.Empty(), DraftState.Draft);

    internal static DictionaryDraft Rehydrate(WordSet words, DraftState state) => new(words, state);

    internal void SetWords(WordSet words)
    {
        Words = words;
        State = DraftState.Draft;
    }

    internal void MarkValidated()
    {
        State = DraftState.Validated;
    }

    internal void MarkDraft()
    {
        State = DraftState.Draft;
    }
}

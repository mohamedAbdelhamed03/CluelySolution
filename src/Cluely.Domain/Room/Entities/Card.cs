using Cluely.Domain.Common;
using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Domain.Room.Entities;

public sealed class Card
{
    public CardPosition Position { get; }
    public string Word { get; }
    public CardOwnership Ownership { get; }
    public bool IsRevealed { get; private set; }

    private Card(CardPosition position, string word, CardOwnership ownership)
    {
        Position = position;
        Word = word;
        Ownership = ownership;
        IsRevealed = false;
    }

    // Internal constructor for rehydration
    internal Card(CardPosition position, string word, CardOwnership ownership, bool isRevealed)
    {
        Position = position;
        Word = word;
        Ownership = ownership;
        IsRevealed = isRevealed;
    }

    public static Card Create(CardPosition position, string word, CardOwnership ownership) => new(position, word, ownership);

    public void Reveal() => IsRevealed = true;
}

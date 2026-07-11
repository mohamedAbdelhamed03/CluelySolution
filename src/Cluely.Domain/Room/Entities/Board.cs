using Cluely.Domain.Common;
using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Domain.Room.Entities;

public sealed class Board
{
    private readonly List<Card> _cards = [];

    public IReadOnlyList<Card> Cards => _cards.AsReadOnly();

    public int RedRemaining { get; private set; }
    public int BlueRemaining { get; private set; }

    internal Board(IEnumerable<Card> cards, int redRemaining, int blueRemaining)
    {
        _cards.AddRange(cards);
        RedRemaining = redRemaining;
        BlueRemaining = blueRemaining;
    }

    public static Board Create(IEnumerable<(CardPosition Position, string Word, CardOwnership Ownership)> cardData)
    {
        var cards = cardData.Select(data => Card.Create(data.Position, data.Word, data.Ownership)).ToList();
        var redCount = cards.Count(c => c.Ownership == CardOwnership.Red && !c.IsRevealed);
        var blueCount = cards.Count(c => c.Ownership == CardOwnership.Blue && !c.IsRevealed);
        return new Board(cards, redCount, blueCount);
    }

    public Card RevealCard(CardPosition position)
    {
        var card = _cards.Single(c => c.Position == position);
        if (card.IsRevealed)
        {
            throw new InvalidOperationException("Card already revealed");
        }

        card.Reveal();
        if (card.Ownership == CardOwnership.Red)
            RedRemaining--;
        else if (card.Ownership == CardOwnership.Blue)
            BlueRemaining--;

        return card;
    }
}

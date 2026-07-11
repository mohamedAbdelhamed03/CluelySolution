using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class CardPosition : ValueObject
{
    public int Value { get; }

    private CardPosition(int value)
    {
        Value = value;
    }

    public static CardPosition From(int value)
    {
        if (value < 0 || value > 24)
        {
            throw new ArgumentException("Card position must be between 0 and 24", nameof(value));
        }

        return new CardPosition(value);
    }

    public int Row => Value / 5;
    public int Col => Value % 5;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

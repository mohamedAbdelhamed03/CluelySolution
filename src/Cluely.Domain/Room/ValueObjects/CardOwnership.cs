using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class CardOwnership : ValueObject
{
    public static readonly CardOwnership Red = new("Red");
    public static readonly CardOwnership Blue = new("Blue");
    public static readonly CardOwnership Neutral = new("Neutral");
    public static readonly CardOwnership Assassin = new("Assassin");

    public string Value { get; }

    private CardOwnership(string value)
    {
        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

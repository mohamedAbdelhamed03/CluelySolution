using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class AggregateVersion : ValueObject
{
    public int Value { get; }

    private AggregateVersion(int value)
    {
        Value = value;
    }

    public static AggregateVersion Initial() => new(0);

    public AggregateVersion Next() => new(Value + 1);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

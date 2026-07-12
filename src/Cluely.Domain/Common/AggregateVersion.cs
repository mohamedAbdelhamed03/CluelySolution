using Cluely.Domain.Common;

namespace Cluely.Domain.Common;

public sealed class AggregateVersion : ValueObject
{
    public int Value { get; }

    private AggregateVersion(int value)
    {
        Value = value;
    }

    public static AggregateVersion Initial() => new(0);

    public static AggregateVersion From(int value) => new(value);

    public AggregateVersion Next() => new(Value + 1);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

using Cluely.Domain.Common;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class VersionLabel : ValueObject, IComparable<VersionLabel>
{
    public int Value { get; }

    private VersionLabel(int value)
    {
        Value = value;
    }

    public static VersionLabel Initial() => new(1);

    public static VersionLabel From(int value)
    {
        if (value < 1)
        {
            throw new ArgumentException("Version label must be at least 1.", nameof(value));
        }

        return new VersionLabel(value);
    }

    public VersionLabel Next() => new(Value + 1);

    public int CompareTo(VersionLabel? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Value.CompareTo(other.Value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

using Cluely.Domain.Common;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class VersionId : ValueObject
{
    public Guid Value { get; }

    private VersionId(Guid value)
    {
        Value = value;
    }

    public static VersionId New() => new(Guid.NewGuid());

    public static VersionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Version id cannot be empty.", nameof(value));
        }

        return new VersionId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

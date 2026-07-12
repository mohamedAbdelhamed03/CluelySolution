using Cluely.Domain.Common;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class DictionaryId : ValueObject
{
    public Guid Value { get; }

    private DictionaryId(Guid value)
    {
        Value = value;
    }

    public static DictionaryId New() => new(Guid.NewGuid());

    public static DictionaryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Dictionary id cannot be empty.", nameof(value));
        }

        return new DictionaryId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

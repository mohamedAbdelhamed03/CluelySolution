using Cluely.Domain.Common;
using Cluely.Domain.Content;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class Region : ValueObject
{
    public string Value { get; }

    private Region(string value)
    {
        Value = value;
    }

    public static Region From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Region is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > DictionaryValidation.MaxRegionLength)
        {
            throw new ArgumentException(
                $"Region exceeds maximum length of {DictionaryValidation.MaxRegionLength}.",
                nameof(value));
        }

        return new Region(trimmed);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

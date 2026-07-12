using Cluely.Domain.Common;
using Cluely.Domain.Content;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class Description : ValueObject
{
    public string Value { get; }

    private Description(string value)
    {
        Value = value;
    }

    public static Description From(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        if (normalized.Length > DictionaryValidation.MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Description exceeds maximum length of {DictionaryValidation.MaxDescriptionLength}.",
                nameof(value));
        }

        return new Description(normalized);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

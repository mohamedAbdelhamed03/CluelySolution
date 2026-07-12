using Cluely.Domain.Common;
using Cluely.Domain.Content;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class Title : ValueObject
{
    public string Value { get; }

    private Title(string value)
    {
        Value = value;
    }

    public static Title From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Title is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > DictionaryValidation.MaxTitleLength)
        {
            throw new ArgumentException(
                $"Title exceeds maximum length of {DictionaryValidation.MaxTitleLength}.",
                nameof(value));
        }

        return new Title(trimmed);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

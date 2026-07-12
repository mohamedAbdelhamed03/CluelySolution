using Cluely.Domain.Common;
using Cluely.Domain.Content;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class Language : ValueObject
{
    public string Value { get; }

    private Language(string value)
    {
        Value = value;
    }

    public static Language From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Language is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > DictionaryValidation.MaxLanguageLength)
        {
            throw new ArgumentException(
                $"Language exceeds maximum length of {DictionaryValidation.MaxLanguageLength}.",
                nameof(value));
        }

        return new Language(trimmed);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

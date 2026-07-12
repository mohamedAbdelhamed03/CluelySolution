using Cluely.Domain.Common;
using Cluely.Domain.Content;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class Word : ValueObject
{
    public string Value { get; }

    private Word(string value)
    {
        Value = value;
    }

    public static Word FromRaw(string raw)
    {
        if (raw is null)
        {
            throw new ArgumentException("Word cannot be null.", nameof(raw));
        }

        var normalized = Normalize(raw);
        if (normalized.Length < DictionaryValidation.MinWordLength)
        {
            throw new ArgumentException("Word cannot be blank.", nameof(raw));
        }

        if (normalized.Length > DictionaryValidation.MaxWordLength)
        {
            throw new ArgumentException(
                $"Word exceeds maximum length of {DictionaryValidation.MaxWordLength}.",
                nameof(raw));
        }

        return new Word(normalized);
    }

    public static string Normalize(string raw)
    {
        var trimmed = raw.Trim();
        var segments = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', segments).ToLowerInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

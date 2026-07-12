using Cluely.Domain.Common;
using Cluely.Domain.Content;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class Tags : ValueObject
{
    public IReadOnlyList<string> Values { get; }

    private Tags(IReadOnlyList<string> values)
    {
        Values = values;
    }

    public static Tags From(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return Empty();
        }

        var normalized = values
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count > DictionaryValidation.MaxTags)
        {
            throw new ArgumentException($"At most {DictionaryValidation.MaxTags} tags are allowed.");
        }

        if (normalized.Any(tag => tag.Length > DictionaryValidation.MaxTagLength))
        {
            throw new ArgumentException(
                $"Each tag must be at most {DictionaryValidation.MaxTagLength} characters.");
        }

        return new Tags(normalized);
    }

    public static Tags Empty() => new([]);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var value in Values)
        {
            yield return value;
        }
    }
}

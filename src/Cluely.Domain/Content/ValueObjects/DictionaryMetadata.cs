using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class DictionaryMetadata : ValueObject
{
    public Title Title { get; }
    public Description Description { get; }
    public Tags Tags { get; }
    public Language Language { get; }
    public Region? Region { get; }

    private DictionaryMetadata(
        Title title,
        Description description,
        Tags tags,
        Language language,
        Region? region)
    {
        Title = title;
        Description = description;
        Tags = tags;
        Language = language;
        Region = region;
    }

    public static DictionaryMetadata Create(
        string title,
        string description,
        IEnumerable<string>? tags,
        string language,
        string? region = null)
    {
        return new DictionaryMetadata(
            Title.From(title),
            Description.From(description),
            Tags.From(tags),
            Language.From(language),
            string.IsNullOrWhiteSpace(region) ? null : Region.From(region));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Title;
        yield return Description;
        yield return Tags;
        yield return Language;
        if (Region is not null)
        {
            yield return Region;
        }
    }
}

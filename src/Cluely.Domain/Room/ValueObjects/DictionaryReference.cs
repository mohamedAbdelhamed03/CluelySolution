using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class DictionaryReference : ValueObject
{
    public RegionCode Region { get; }
    public ContentVersion Version { get; }

    private DictionaryReference(RegionCode region, ContentVersion version)
    {
        Region = region;
        Version = version;
    }

    public static DictionaryReference Create(RegionCode region, ContentVersion version) => new(region, version);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Region;
        yield return Version;
    }
}

public sealed class RegionCode : ValueObject
{
    public string Value { get; }

    private RegionCode(string value)
    {
        Value = value;
    }

    public static RegionCode From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Region code cannot be empty", nameof(value));
        }

        return new RegionCode(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }
}

public sealed class ContentVersion : ValueObject
{
    public string Value { get; }

    private ContentVersion(string value)
    {
        Value = value;
    }

    public static ContentVersion From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Content version cannot be empty", nameof(value));
        }

        return new ContentVersion(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

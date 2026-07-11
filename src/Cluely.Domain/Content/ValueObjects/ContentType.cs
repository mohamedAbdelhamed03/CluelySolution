using Cluely.Domain.Common;

namespace Cluely.Domain.Content.ValueObjects;

/// <summary>
/// The type of a Dictionary, distinguishing who owns it (ADR-011 §6, BR-CONTENT-040). Every type
/// shares the same lifecycle, immutability, versioning, and pinning; types differ only in ownership
/// and visibility (AI-CP-14). Only the two types in scope for the first capability are modelled here;
/// further types (organization, community, premium, …) are added additively per ADR-011 §10.
/// A content type must be explicit — unlike visibility, there is no safe default.
/// </summary>
public sealed class ContentType : ValueObject
{
    /// <summary>Platform-owned content; retains its existing governance, including one-per-region (DM-C1).</summary>
    public static readonly ContentType Official = new("Official");

    /// <summary>Content owned by an individual account; owner-scoped and many-per-owner.</summary>
    public static readonly ContentType User = new("User");

    /// <summary>The canonical string form of this content type.</summary>
    public string Value { get; }

    private ContentType(string value)
    {
        Value = value;
    }

    /// <summary>Parses a content type. The value must be specified explicitly.</summary>
    /// <exception cref="ArgumentException">Thrown when the value is blank or not a known type.</exception>
    public static ContentType From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Content type must be specified.", nameof(value));
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "official" => Official,
            "user" => User,
            _ => throw new ArgumentException("Invalid content type value.", nameof(value))
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

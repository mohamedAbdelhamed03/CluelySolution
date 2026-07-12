using Cluely.Domain.Common;

namespace Cluely.Domain.Content.ValueObjects;

/// <summary>
/// How a dictionary's content originated (ADR-011 §19). Only <see cref="Clone"/> is modelled here,
/// because cloning is the only origin that records provenance in this capability; further origins
/// (import, official-seed) are added additively when those capabilities are implemented.
/// </summary>
public sealed class OriginType : ValueObject
{
    /// <summary>Content seeded from another dictionary's published version.</summary>
    public static readonly OriginType Clone = new("Clone");

    /// <summary>The canonical string form of this origin type.</summary>
    public string Value { get; }

    private OriginType(string value)
    {
        Value = value;
    }

    /// <summary>Parses an origin type. The value must be specified and recognized.</summary>
    /// <exception cref="ArgumentException">Thrown when the value is blank or not a known origin type.</exception>
    public static OriginType From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Origin type must be specified.", nameof(value));
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "clone" => Clone,
            _ => throw new ArgumentException("Invalid origin type value.", nameof(value))
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

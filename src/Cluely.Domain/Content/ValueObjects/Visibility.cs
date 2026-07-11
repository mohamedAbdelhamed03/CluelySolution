using Cluely.Domain.Common;

namespace Cluely.Domain.Content.ValueObjects;

/// <summary>
/// The visibility of a Dictionary (Feature Specification §7). The default is <see cref="Private"/>
/// (BR-CONTENT-030) — a Dictionary is never public by accident. Organization visibility is reserved
/// for a future capability and is deliberately not modelled here.
/// </summary>
public sealed class Visibility : ValueObject
{
    /// <summary>Visible and selectable only to the owner. The default.</summary>
    public static readonly Visibility Private = new("Private");

    /// <summary>Additionally visible/selectable to accounts on the dictionary's share list.</summary>
    public static readonly Visibility Shared = new("Shared");

    /// <summary>Discoverable and selectable by anyone once published and approved.</summary>
    public static readonly Visibility Public = new("Public");

    /// <summary>The canonical string form of this visibility level.</summary>
    public string Value { get; }

    private Visibility(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Parses a visibility level. A blank value resolves to <see cref="Private"/> (the safe default,
    /// BR-CONTENT-030).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a non-blank value is not a known level.</exception>
    public static Visibility From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Private;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "private" => Private,
            "shared" => Shared,
            "public" => Public,
            _ => throw new ArgumentException("Invalid visibility value.", nameof(value))
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

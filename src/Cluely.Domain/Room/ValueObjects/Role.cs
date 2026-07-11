using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class Role : ValueObject
{
    public static readonly Role Spymaster = new("Spymaster");
    public static readonly Role Operative = new("Operative");

    public string Value { get; }

    private Role(string value)
    {
        Value = value;
    }

    public static Role From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Operative;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "spymaster" => Spymaster,
            "operative" => Operative,
            _ => throw new ArgumentException("Invalid role value", nameof(value))
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

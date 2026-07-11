using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class Team : ValueObject
{
    public static readonly Team Red = new("Red");
    public static readonly Team Blue = new("Blue");
    public static readonly Team Unassigned = new("Unassigned");

    public string Value { get; }

    private Team(string value)
    {
        Value = value;
    }

    public static Team From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Unassigned;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "red" => Red,
            "blue" => Blue,
            _ => throw new ArgumentException("Invalid team value", nameof(value))
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

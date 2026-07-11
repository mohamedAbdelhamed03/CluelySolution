using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class RoomCode : ValueObject
{
    public string Value { get; }

    private RoomCode(string value)
    {
        Value = value;
    }

    public static RoomCode From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Room code cannot be empty", nameof(value));
        }

        return new RoomCode(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }
}

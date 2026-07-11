using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class RoomId : ValueObject
{
    public Guid Value { get; }

    private RoomId(Guid value)
    {
        Value = value;
    }

    public static RoomId New() => new(Guid.NewGuid());

    public static RoomId From(Guid value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

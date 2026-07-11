using Cluely.Domain.Common;

namespace Cluely.Domain.Room.ValueObjects;

public sealed class ParticipantId : ValueObject
{
    public Guid Value { get; }

    private ParticipantId(Guid value)
    {
        Value = value;
    }

    public static ParticipantId New() => new(Guid.NewGuid());

    public static ParticipantId From(Guid value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

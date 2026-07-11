using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Domain.Common;

public interface IRoomDomainEvent : IDomainEvent
{
    RoomId RoomId { get; }
}

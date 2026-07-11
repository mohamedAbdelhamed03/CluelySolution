using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Delivery.Contracts;
using Cluely.Infrastructure.Delivery.Projections;

namespace Cluely.Infrastructure.Delivery.Visibility;

public interface IVisibilityFilter
{
    RoomProjectionDto Filter(InternalRoomProjection projection, Role role, Team team);
}

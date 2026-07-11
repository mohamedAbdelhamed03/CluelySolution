using Cluely.Domain.Room;

namespace Cluely.Infrastructure.Delivery.Projections;

public interface IProjectionBuilder
{
    InternalRoomProjection Build(Room room);
}

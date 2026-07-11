namespace Cluely.Infrastructure.Delivery.Contracts;

public sealed record DeliveryEnvelope<TProjection>(
    Guid RoomId,
    int AggregateVersion,
    bool IsSnapshot,
    TProjection Projection);

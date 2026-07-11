using Cluely.Domain.Room.Events;

namespace Cluely.Application.Common.Ports;

public interface IDomainEventPublisher
{
    Task PublishAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default);
}
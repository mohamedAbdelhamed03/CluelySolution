using Cluely.Application.Common.Ports;
using Cluely.Domain.Common;

namespace Cluely.IntegrationTests.Infrastructure;

/// <summary>
/// Swallows domain events in API integration tests so room-only SignalR delivery is not required.
/// </summary>
internal sealed class NoOpDomainEventPublisher : IDomainEventPublisher
{
    public Task PublishAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

using Cluely.Application.Common.Ports;
using Cluely.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Cluely.Infrastructure.Delivery.Dispatch;

/// <summary>
/// Routes committed domain events to the room (SignalR) or content in-process publishers.
/// </summary>
public sealed class CompositeDomainEventPublisher : IDomainEventPublisher
{
    private readonly SignalRDomainEventPublisher _roomPublisher;
    private readonly ContentDomainEventPublisher _contentPublisher;
    private readonly ILogger<CompositeDomainEventPublisher> _logger;

    public CompositeDomainEventPublisher(
        SignalRDomainEventPublisher roomPublisher,
        ContentDomainEventPublisher contentPublisher,
        ILogger<CompositeDomainEventPublisher> logger)
    {
        _roomPublisher = roomPublisher;
        _contentPublisher = contentPublisher;
        _logger = logger;
    }

    public async Task PublishAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return;
        }

        var roomEvents = events.OfType<IRoomDomainEvent>().Cast<IDomainEvent>().ToList();
        var contentEvents = events.OfType<IContentDomainEvent>().Cast<IDomainEvent>().ToList();

        if (roomEvents.Count > 0)
        {
            await _roomPublisher.PublishAsync(roomEvents, cancellationToken);
        }

        if (contentEvents.Count > 0)
        {
            await _contentPublisher.PublishAsync(contentEvents, cancellationToken);
        }

        var published = roomEvents.Count + contentEvents.Count;
        if (published != events.Count)
        {
            _logger.LogWarning(
                "Skipped {SkippedCount} domain events that are neither room nor content events.",
                events.Count - published);
        }
    }
}

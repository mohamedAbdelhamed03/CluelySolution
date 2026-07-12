using Cluely.Application.Common.Ports;
using Cluely.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Cluely.Infrastructure.Delivery.Dispatch;

/// <summary>
/// In-process publisher for committed <see cref="IContentDomainEvent"/> instances (TD-012).
/// </summary>
public sealed class ContentDomainEventPublisher : IDomainEventPublisher
{
    private readonly ILogger<ContentDomainEventPublisher> _logger;

    public ContentDomainEventPublisher(ILogger<ContentDomainEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            if (domainEvent is not IContentDomainEvent contentEvent)
            {
                throw new InvalidOperationException(
                    $"Domain event '{domainEvent.GetType().Name}' does not implement {nameof(IContentDomainEvent)}.");
            }

            _logger.LogInformation(
                "Published content domain event {EventType} for dictionary {DictionaryId}.",
                domainEvent.GetType().Name,
                contentEvent.DictionaryId.Value);
        }

        return Task.CompletedTask;
    }
}

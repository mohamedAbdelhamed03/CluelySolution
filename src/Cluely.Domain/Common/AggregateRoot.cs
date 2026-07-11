using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Domain.Common;

public abstract class AggregateRoot<TId>
    where TId : ValueObject
{
    private readonly List<IDomainEvent> _pendingEvents = [];

    public TId Id { get; }
    public AggregateVersion Version { get; private set; }

    protected AggregateRoot(TId id)
    {
        Id = id;
        Version = AggregateVersion.Initial();
    }

    // Internal constructor for rehydration
    internal AggregateRoot(TId id, AggregateVersion version)
    {
        Id = id;
        Version = version;
    }

    public IReadOnlyList<IDomainEvent> GetPendingEvents() => _pendingEvents.AsReadOnly();

    public void ClearPendingEvents() => _pendingEvents.Clear();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _pendingEvents.Add(domainEvent);

    protected void IncrementVersion() => Version = Version.Next();
}

using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;
using DictionaryAggregate = Cluely.Domain.Content.Dictionary;

namespace Cluely.UnitTests.Content.Handlers;

internal sealed class FakeDictionaryRepository : IDictionaryRepository
{
    private readonly System.Collections.Generic.Dictionary<DictionaryId, DictionaryAggregate> _dictionaries = new();
    private readonly System.Collections.Generic.Dictionary<Guid, DictionaryId> _idempotencyKeys = new();

    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<DictionaryAggregate?> GetAsync(DictionaryId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_dictionaries.GetValueOrDefault(id));

    public Task<DictionaryAggregate?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (_idempotencyKeys.TryGetValue(idempotencyKey, out var dictionaryId))
        {
            return GetAsync(dictionaryId, cancellationToken);
        }

        return Task.FromResult<DictionaryAggregate?>(null);
    }

    public Task AddAsync(DictionaryAggregate dictionary, Guid idempotencyKey, CancellationToken cancellationToken = default)
    {
        AddCount++;
        _dictionaries[dictionary.Id] = dictionary;
        _idempotencyKeys[idempotencyKey] = dictionary.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DictionaryAggregate dictionary, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        _dictionaries[dictionary.Id] = dictionary;
        return Task.CompletedTask;
    }

    public void ResetCounters()
    {
        AddCount = 0;
        UpdateCount = 0;
    }

    public void Seed(DictionaryAggregate dictionary)
    {
        _dictionaries[dictionary.Id] = dictionary;
    }

    public void SeedWithIdempotency(DictionaryAggregate dictionary, Guid idempotencyKey)
    {
        Seed(dictionary);
        _idempotencyKeys[idempotencyKey] = dictionary.Id;
    }
}

internal sealed class FakeDomainEventPublisher : IDomainEventPublisher
{
    public List<IDomainEvent> PublishedEvents { get; } = [];

    public Task PublishAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        PublishedEvents.AddRange(events);
        return Task.CompletedTask;
    }
}

internal sealed class FakeCurrentUserAccessor : ICurrentUserAccessor
{
    public Guid? UserId { get; set; }

    public bool IsAuthenticated => UserId.HasValue;
}

internal sealed class FakeContentModeratorAccessor : IContentModeratorAccessor
{
    public bool IsModerator { get; set; }
}

internal sealed class FakeGuidGenerator : IGuidGenerator
{
    private readonly Queue<Guid> _ids = new();

    public void Enqueue(Guid id) => _ids.Enqueue(id);

    public Guid Generate() => _ids.Count > 0 ? _ids.Dequeue() : Guid.NewGuid();
}

using Cluely.Application.Common.Ports.Content;
using Cluely.Domain.Content;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Infrastructure.Content;

internal sealed class UnavailableDictionaryRepository : IDictionaryRepository
{
    private static InvalidOperationException NotConfigured() =>
        new("Dictionary persistence is not configured until Slice 10.");

    public Task<Dictionary?> GetAsync(DictionaryId id, CancellationToken cancellationToken = default) =>
        throw NotConfigured();

    public Task<Dictionary?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default) =>
        throw NotConfigured();

    public Task AddAsync(Dictionary dictionary, Guid idempotencyKey, CancellationToken cancellationToken = default) =>
        throw NotConfigured();

    public Task UpdateAsync(Dictionary dictionary, CancellationToken cancellationToken = default) =>
        throw NotConfigured();
}

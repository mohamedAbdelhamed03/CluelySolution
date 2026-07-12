using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.ReadModels;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Infrastructure.ReadModels;

/// <summary>
/// Interim discovery read-model provider. The discovery catalog is served from a persistence-backed
/// read store (projection) that is introduced with the content persistence slice; until then this
/// adapter exposes no content. It performs no mutation, publishes no events, and returns only
/// client-safe read models. Tracked as TD-019.
/// </summary>
public sealed class DictionaryReadModelProvider : IDictionaryReadModelProvider
{
    public Task<IReadOnlyList<DictionarySummaryReadModel>> GetOwnedAsync(
        OwnerId owner,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DictionarySummaryReadModel>>([]);
    }

    public Task<IReadOnlyList<DictionarySummaryReadModel>> GetDiscoverableAsync(
        OwnerId requester,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DictionarySummaryReadModel>>([]);
    }

    public Task<DictionaryDetailsReadModel?> GetDetailsAsync(
        DictionaryId dictionaryId,
        OwnerId requester,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DictionaryDetailsReadModel?>(null);
    }

    public Task<IReadOnlyList<DictionaryVersionReadModel>?> GetVersionsAsync(
        DictionaryId dictionaryId,
        OwnerId requester,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DictionaryVersionReadModel>?>(null);
    }
}

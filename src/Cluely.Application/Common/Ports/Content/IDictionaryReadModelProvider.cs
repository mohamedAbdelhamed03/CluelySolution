using Cluely.Application.Common.ReadModels;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Application.Common.Ports.Content;

/// <summary>
/// Read-only provider for dictionary discovery. Every method applies visibility filtering before
/// returning results: private content is visible only to its owner, shared content to its grantees,
/// and public content to any authenticated requester (see <c>DictionaryVisibilityPolicy</c>).
/// Implementations never mutate aggregates, never publish events, and never expose domain entities.
/// </summary>
public interface IDictionaryReadModelProvider
{
    /// <summary>Dictionaries owned by <paramref name="owner"/> (any visibility or lifecycle state).</summary>
    Task<IReadOnlyList<DictionarySummaryReadModel>> GetOwnedAsync(
        OwnerId owner,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dictionaries the requester may discover but does not own: public dictionaries and dictionaries
    /// shared with the requester.
    /// </summary>
    Task<IReadOnlyList<DictionarySummaryReadModel>> GetDiscoverableAsync(
        OwnerId requester,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detail view of a dictionary the requester is permitted to see, or <c>null</c> when it does not
    /// exist or the requester is not authorized (indistinguishable, to avoid existence enumeration).
    /// </summary>
    Task<DictionaryDetailsReadModel?> GetDetailsAsync(
        DictionaryId dictionaryId,
        OwnerId requester,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Versions of a dictionary the requester is permitted to see (full history for the owner; the
    /// current published version only for other authorized viewers), or <c>null</c> when the
    /// dictionary does not exist or the requester is not authorized.
    /// </summary>
    Task<IReadOnlyList<DictionaryVersionReadModel>?> GetVersionsAsync(
        DictionaryId dictionaryId,
        OwnerId requester,
        CancellationToken cancellationToken = default);
}

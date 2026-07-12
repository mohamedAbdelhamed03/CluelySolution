using Cluely.Application.Content.PublishDictionary;

namespace Cluely.Application.Common.Ports.Content;

/// <summary>
/// Persists command outcomes so retried mutations (e.g. publish) replay deterministically (REC-8 / TD-017).
/// </summary>
public interface IContentCommandIdempotencyStore
{
    Task<PublishDictionaryResult?> TryGetPublishOutcomeAsync(
        Guid idempotencyKey,
        CancellationToken cancellationToken = default);

    Task SavePublishOutcomeAsync(
        Guid idempotencyKey,
        PublishDictionaryResult outcome,
        CancellationToken cancellationToken = default);
}

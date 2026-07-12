namespace Cluely.Application.Content.PublishDictionary;

public sealed record PublishDictionaryCommand(
    Guid DictionaryId,
    Guid CorrelationId,
    Guid IdempotencyKey,
    DateTime? PublishedAt = null);

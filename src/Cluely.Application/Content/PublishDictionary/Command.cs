namespace Cluely.Application.Content.PublishDictionary;

public sealed record PublishDictionaryCommand(
    Guid DictionaryId,
    Guid CorrelationId,
    DateTime? PublishedAt = null);

namespace Cluely.Application.Content.PublishDictionary;

public sealed record PublishDictionaryCommand(
    Guid DictionaryId,
    Guid VersionId,
    Guid CorrelationId,
    DateTime? PublishedAt = null);

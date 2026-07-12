namespace Cluely.Application.Content.CloneDictionary;

public sealed record CloneDictionaryCommand(
    Guid SourceDictionaryId,
    Guid SourceVersionId,
    Guid CorrelationId,
    Guid IdempotencyKey);

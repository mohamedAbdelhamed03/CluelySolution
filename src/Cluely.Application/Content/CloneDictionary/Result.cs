namespace Cluely.Application.Content.CloneDictionary;

public sealed record CloneDictionaryResult(
    Guid DictionaryId,
    Guid SourceDictionaryId,
    Guid SourceVersionId);

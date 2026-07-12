namespace Cluely.Application.Content.PublishDictionary;

public sealed record PublishDictionaryResult(
    Guid DictionaryId,
    Guid VersionId,
    int VersionLabel,
    int WordCount);

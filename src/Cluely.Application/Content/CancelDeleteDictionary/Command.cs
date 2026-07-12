namespace Cluely.Application.Content.CancelDeleteDictionary;

public sealed record CancelDeleteDictionaryCommand(Guid DictionaryId, Guid CorrelationId);

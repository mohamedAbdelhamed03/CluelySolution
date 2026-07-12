namespace Cluely.Application.Content.DeleteDictionary;

public sealed record DeleteDictionaryCommand(Guid DictionaryId, Guid CorrelationId);

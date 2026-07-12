namespace Cluely.Application.Content.RestoreDictionary;

public sealed record RestoreDictionaryCommand(Guid DictionaryId, Guid CorrelationId);

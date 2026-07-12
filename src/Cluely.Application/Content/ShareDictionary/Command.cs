namespace Cluely.Application.Content.ShareDictionary;

public sealed record ShareDictionaryCommand(Guid DictionaryId, Guid GranteeId, Guid CorrelationId);

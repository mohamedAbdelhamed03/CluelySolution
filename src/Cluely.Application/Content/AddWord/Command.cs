namespace Cluely.Application.Content.AddWord;

public sealed record AddWordCommand(Guid DictionaryId, string Word, Guid CorrelationId);

namespace Cluely.Application.Content.RemoveWord;

public sealed record RemoveWordCommand(Guid DictionaryId, string Word, Guid CorrelationId);

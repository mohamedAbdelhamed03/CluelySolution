namespace Cluely.Application.Content.ReplaceWord;

public sealed record ReplaceWordCommand(
    Guid DictionaryId,
    string ExistingWord,
    string NewWord,
    Guid CorrelationId);

namespace Cluely.Application.Content.BulkAddWords;

public sealed record BulkAddWordsResult(Guid DictionaryId, int WordCount, int WordsAdded);

namespace Cluely.Application.Content.BulkAddWords;

public sealed record BulkAddWordsCommand(
    Guid DictionaryId,
    IReadOnlyList<string> Words,
    Guid CorrelationId);

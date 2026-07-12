namespace Cluely.Application.Content.RenameDictionary;

public sealed record RenameDictionaryCommand(
    Guid DictionaryId,
    string Title,
    string Description,
    IReadOnlyList<string>? Tags,
    string Language,
    string? Region,
    Guid CorrelationId);

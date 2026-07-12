namespace Cluely.Application.Content.CreateDictionary;

public sealed record CreateDictionaryCommand(
    string Title,
    string Description,
    IReadOnlyList<string>? Tags,
    string Language,
    string? Region,
    string ContentType,
    Guid CorrelationId,
    Guid IdempotencyKey);

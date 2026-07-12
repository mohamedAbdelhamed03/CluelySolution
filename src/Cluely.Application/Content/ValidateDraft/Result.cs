namespace Cluely.Application.Content.ValidateDraft;

public sealed record ValidateDraftResult(
    Guid DictionaryId,
    bool IsValid,
    IReadOnlyList<string> Errors,
    int WordCount);

namespace Cluely.Application.Common.ReadModels;

/// <summary>
/// Client-safe summary of a dictionary for discovery catalogs. Contains no domain entities, no draft
/// words, and no share list — visibility filtering is applied by the read-model provider before a
/// summary is produced.
/// </summary>
public sealed record DictionarySummaryReadModel(
    Guid DictionaryId,
    Guid OwnerId,
    string Title,
    string Description,
    IReadOnlyList<string> Tags,
    string Language,
    string? Region,
    string Visibility,
    string ContentType,
    Guid? CurrentVersionId,
    int? CurrentVersionLabel);

/// <summary>
/// Client-safe detail view of a dictionary, including the versions the requester is permitted to see
/// (full history for the owner; the current published version only for other authorized viewers).
/// </summary>
public sealed record DictionaryDetailsReadModel(
    Guid DictionaryId,
    Guid OwnerId,
    string Title,
    string Description,
    IReadOnlyList<string> Tags,
    string Language,
    string? Region,
    string Visibility,
    string ContentType,
    Guid? CurrentVersionId,
    int? CurrentVersionLabel,
    IReadOnlyList<DictionaryVersionReadModel> Versions);

/// <summary>Client-safe summary of a single published dictionary version. Excludes the word list.</summary>
public sealed record DictionaryVersionReadModel(
    Guid VersionId,
    int Label,
    DateTime PublishedAt,
    int WordCount,
    string LifecycleState);

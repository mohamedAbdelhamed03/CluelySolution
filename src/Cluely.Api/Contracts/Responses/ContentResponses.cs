namespace Cluely.Api.Contracts.Responses;

/// <summary>Response returned when a dictionary is created.</summary>
public sealed record ContentCreatedResponse(Guid DictionaryId, string Title);

/// <summary>Response returned when a dictionary's metadata is updated.</summary>
public sealed record ContentUpdatedResponse(Guid DictionaryId, string Title);

/// <summary>Response returned after a draft word-count change.</summary>
public sealed record WordCountResponse(Guid DictionaryId, int WordCount);

/// <summary>Response returned when a draft is validated.</summary>
public sealed record ValidateContentResponse(
    Guid DictionaryId,
    bool IsValid,
    IReadOnlyList<string> Errors,
    int WordCount);

/// <summary>Response returned when a dictionary is published.</summary>
public sealed record PublishContentResponse(
    Guid DictionaryId,
    Guid VersionId,
    int VersionLabel,
    int WordCount);

/// <summary>Response returned when a dictionary is cloned.</summary>
public sealed record CloneContentResponse(
    Guid DictionaryId,
    Guid SourceDictionaryId,
    Guid SourceVersionId);

/// <summary>Discovery summary of a dictionary.</summary>
public sealed record ContentSummaryResponse(
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

/// <summary>Summary of a single published dictionary version.</summary>
public sealed record ContentVersionResponse(
    Guid VersionId,
    int Label,
    DateTime PublishedAt,
    int WordCount,
    string LifecycleState);

/// <summary>Detail view of a dictionary and the versions the caller may see.</summary>
public sealed record ContentDetailsResponse(
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
    IReadOnlyList<ContentVersionResponse> Versions);

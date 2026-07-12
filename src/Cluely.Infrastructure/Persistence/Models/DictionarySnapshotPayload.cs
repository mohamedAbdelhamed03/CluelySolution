namespace Cluely.Infrastructure.Persistence.Models;

/// <summary>Serialized full state of a Dictionary aggregate (the parts not needed as query columns).</summary>
internal sealed class DictionarySnapshotPayload
{
    public string ContentType { get; init; } = string.Empty;
    public string Visibility { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public DictionaryMetadataPayload Metadata { get; init; } = new();
    public DictionaryDraftPayload Draft { get; init; } = new();
    public DictionaryProvenancePayload? Provenance { get; init; }
    public Guid? CurrentVersionId { get; init; }
    public int NextVersionLabel { get; init; }
    public List<DictionaryVersionPayload> Versions { get; init; } = [];
    public List<DictionaryShareGrantPayload> ShareGrants { get; init; } = [];
}

internal sealed class DictionaryMetadataPayload
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public string Language { get; init; } = string.Empty;
    public string? Region { get; init; }
}

internal sealed class DictionaryDraftPayload
{
    public List<string> Words { get; init; } = [];
    public string State { get; init; } = string.Empty;
}

internal sealed class DictionaryVersionPayload
{
    public Guid VersionId { get; init; }
    public int Label { get; init; }
    public List<string> Words { get; init; } = [];
    public string LifecycleState { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
}

internal sealed class DictionaryShareGrantPayload
{
    public Guid GranteeId { get; init; }
    public DateTime GrantedAt { get; init; }
}

internal sealed class DictionaryProvenancePayload
{
    public Guid SourceDictionaryId { get; init; }
    public Guid SourceVersionId { get; init; }
    public string OriginType { get; init; } = string.Empty;
    public DateTime ClonedAt { get; init; }
}

namespace Cluely.Infrastructure.Persistence.Models;

/// <summary>
/// Persistence row for a Content Platform dictionary. Discovery-queryable attributes are stored as
/// columns so read models can be projected directly (without rehydrating the aggregate); the full
/// aggregate state (draft, versions, share grants, provenance, metadata) is serialized in
/// <see cref="SerializedState"/>. <see cref="Version"/> is the optimistic-concurrency token.
/// </summary>
public class DictionarySnapshot
{
    public Guid DictionaryId { get; set; }
    public Guid OwnerId { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string TagsJson { get; set; } = "[]";

    public Guid? CurrentVersionId { get; set; }
    public int? CurrentVersionLabel { get; set; }

    public int Version { get; set; }
    public Guid? IdempotencyKey { get; set; }

    public int SnapshotSchemaVersion { get; set; }
    public string SerializedState { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }

    public List<DictionaryShareGrantRow> ShareGrants { get; set; } = [];
}

/// <summary>
/// Queryable projection of a dictionary share grant, kept in sync with the aggregate so discovery can
/// filter "shared with me" in SQL without deserializing the aggregate.
/// </summary>
public class DictionaryShareGrantRow
{
    public Guid DictionaryId { get; set; }
    public Guid GranteeId { get; set; }
}

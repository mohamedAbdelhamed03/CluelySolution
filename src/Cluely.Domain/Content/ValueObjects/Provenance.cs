using Cluely.Domain.Common;

namespace Cluely.Domain.Content.ValueObjects;

/// <summary>
/// Immutable, informational record of where a cloned dictionary's content came from (ADR-011 §19,
/// VM-7). It is a non-retaining reference — it never keeps the source alive and may dangle if the
/// source is later deleted. Created once at clone time and never changed.
/// </summary>
public sealed class Provenance : ValueObject
{
    public DictionaryId SourceDictionaryId { get; }
    public VersionId SourceVersionId { get; }
    public OriginType OriginType { get; }
    public DateTime ClonedAt { get; }

    private Provenance(
        DictionaryId sourceDictionaryId,
        VersionId sourceVersionId,
        OriginType originType,
        DateTime clonedAt)
    {
        SourceDictionaryId = sourceDictionaryId;
        SourceVersionId = sourceVersionId;
        OriginType = originType;
        ClonedAt = clonedAt;
    }

    /// <summary>Creates clone provenance referencing the source dictionary and published version.</summary>
    public static Provenance ForClone(
        DictionaryId sourceDictionaryId,
        VersionId sourceVersionId,
        DateTime clonedAt)
    {
        return new Provenance(sourceDictionaryId, sourceVersionId, OriginType.Clone, clonedAt);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return SourceDictionaryId;
        yield return SourceVersionId;
        yield return OriginType;
        yield return ClonedAt;
    }
}

using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class Provenance : ValueObject
{
    public VersionId SourceVersionId { get; }

    private Provenance(VersionId sourceVersionId)
    {
        SourceVersionId = sourceVersionId;
    }

    public static Provenance From(VersionId sourceVersionId) => new(sourceVersionId);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return SourceVersionId;
    }
}

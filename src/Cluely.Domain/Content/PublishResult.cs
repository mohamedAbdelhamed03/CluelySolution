using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Content;

public sealed record PublishResult(
    VersionId VersionId,
    int VersionLabel,
    int WordCount);

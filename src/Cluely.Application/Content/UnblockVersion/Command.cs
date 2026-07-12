namespace Cluely.Application.Content.UnblockVersion;

public sealed record UnblockVersionCommand(Guid DictionaryId, Guid VersionId, Guid CorrelationId);

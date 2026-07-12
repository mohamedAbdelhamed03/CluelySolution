namespace Cluely.Application.Content.BlockVersion;

public sealed record BlockVersionCommand(Guid DictionaryId, Guid VersionId, Guid CorrelationId);

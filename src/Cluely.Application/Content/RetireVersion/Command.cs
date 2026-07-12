namespace Cluely.Application.Content.RetireVersion;

public sealed record RetireVersionCommand(Guid DictionaryId, Guid VersionId, Guid CorrelationId);

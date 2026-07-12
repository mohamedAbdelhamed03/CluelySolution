namespace Cluely.Application.Content.RevokeShare;

public sealed record RevokeShareCommand(Guid DictionaryId, Guid GranteeId, Guid CorrelationId);

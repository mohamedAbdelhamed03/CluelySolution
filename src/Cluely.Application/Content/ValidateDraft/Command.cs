namespace Cluely.Application.Content.ValidateDraft;

public sealed record ValidateDraftCommand(Guid DictionaryId, Guid CorrelationId);

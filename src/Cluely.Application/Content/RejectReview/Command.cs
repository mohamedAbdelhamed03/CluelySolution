namespace Cluely.Application.Content.RejectReview;

public sealed record RejectReviewCommand(Guid DictionaryId, Guid VersionId, Guid CorrelationId);

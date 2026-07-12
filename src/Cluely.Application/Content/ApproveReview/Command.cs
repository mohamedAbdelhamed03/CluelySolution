namespace Cluely.Application.Content.ApproveReview;

public sealed record ApproveReviewCommand(Guid DictionaryId, Guid VersionId, Guid CorrelationId);

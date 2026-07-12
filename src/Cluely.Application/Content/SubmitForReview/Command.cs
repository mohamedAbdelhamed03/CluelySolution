namespace Cluely.Application.Content.SubmitForReview;

public sealed record SubmitForReviewCommand(Guid DictionaryId, Guid VersionId, Guid CorrelationId);

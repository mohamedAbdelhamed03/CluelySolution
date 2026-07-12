namespace Cluely.Infrastructure.Persistence.Models;

public sealed class ContentCommandOutcome
{
    public Guid IdempotencyKey { get; set; }

    public string CommandName { get; set; } = string.Empty;

    public Guid DictionaryId { get; set; }

    public Guid VersionId { get; set; }

    public int VersionLabel { get; set; }

    public int WordCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

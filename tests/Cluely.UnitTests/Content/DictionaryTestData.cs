using Cluely.Domain.Content;
using Cluely.Domain.Content.Entities;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.UnitTests.Content;

internal static class DictionaryTestData
{
    public static DictionaryMetadata DefaultMetadata() =>
        DictionaryMetadata.Create(
            "Test Dictionary",
            "A dictionary used in unit tests.",
            ["party", "family"],
            "en",
            "US");

    public static IEnumerable<string> ValidWordBatch(int count, string prefix = "word")
    {
        for (var index = 0; index < count; index++)
        {
            yield return $"{prefix}{index + 1}";
        }
    }

    public static void ValidateAndPublish(
        Dictionary dictionary,
        OwnerId owner,
        VersionId versionId,
        DateTime publishedAt)
    {
        var report = dictionary.ValidateDraft(owner);
        if (!report.IsValid)
        {
            throw new InvalidOperationException(string.Join(", ", report.Errors));
        }

        dictionary.Publish(owner, versionId, publishedAt);
    }
}

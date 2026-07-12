using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

public sealed class DictionaryMetadataTests
{
    [Fact]
    public void Create_ShouldExposeAllFields()
    {
        var metadata = DictionaryMetadata.Create(
            "Party Pack",
            "Fun words",
            ["party", "family"],
            "en",
            "US");

        metadata.Title.Value.Should().Be("Party Pack");
        metadata.Description.Value.Should().Be("Fun words");
        metadata.Tags.Values.Should().BeEquivalentTo(["party", "family"]);
        metadata.Language.Value.Should().Be("en");
        metadata.Region!.Value.Should().Be("US");
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        Action action = () => DictionaryMetadata.Create("", "desc", [], "en");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Metadata_WithSameValues_ShouldBeEqual()
    {
        var first = DictionaryMetadata.Create("A", "desc", ["x"], "en");
        var second = DictionaryMetadata.Create("A", "desc", ["x"], "en");

        first.Should().Be(second);
    }
}

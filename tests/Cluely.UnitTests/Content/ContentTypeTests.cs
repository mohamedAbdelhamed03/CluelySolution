using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

public class ContentTypeTests
{
    [Theory]
    [InlineData("Official", "Official")]
    [InlineData("official", "Official")]
    [InlineData("  USER  ", "User")]
    public void From_KnownValue_ShouldParseCaseInsensitively(string input, string expected)
    {
        var contentType = ContentType.From(input);

        contentType.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_BlankValue_ShouldThrow(string? input)
    {
        Action action = () => ContentType.From(input!);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void From_UnknownValue_ShouldThrow()
    {
        Action action = () => ContentType.From("premium");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SameType_ShouldBeEqual()
    {
        ContentType.From("user").Should().Be(ContentType.User);
        (ContentType.From("user") == ContentType.User).Should().BeTrue();
        ContentType.From("user").GetHashCode().Should().Be(ContentType.User.GetHashCode());
    }

    [Fact]
    public void DifferentTypes_ShouldNotBeEqual()
    {
        ContentType.Official.Should().NotBe(ContentType.User);
        (ContentType.Official != ContentType.User).Should().BeTrue();
    }
}

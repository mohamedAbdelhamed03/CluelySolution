using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

public class VisibilityTests
{
    [Theory]
    [InlineData("Private", "Private")]
    [InlineData("private", "Private")]
    [InlineData("  SHARED  ", "Shared")]
    [InlineData("public", "Public")]
    public void From_KnownValue_ShouldParseCaseInsensitively(string input, string expected)
    {
        var visibility = Visibility.From(input);

        visibility.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_BlankValue_ShouldDefaultToPrivate(string? input)
    {
        var visibility = Visibility.From(input!);

        visibility.Should().Be(Visibility.Private);
    }

    [Fact]
    public void From_UnknownValue_ShouldThrow()
    {
        Action action = () => Visibility.From("organization");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SameLevel_ShouldBeEqual()
    {
        Visibility.From("public").Should().Be(Visibility.Public);
        (Visibility.From("public") == Visibility.Public).Should().BeTrue();
        Visibility.From("public").GetHashCode().Should().Be(Visibility.Public.GetHashCode());
    }

    [Fact]
    public void DifferentLevels_ShouldNotBeEqual()
    {
        Visibility.Private.Should().NotBe(Visibility.Public);
        (Visibility.Private != Visibility.Shared).Should().BeTrue();
    }
}

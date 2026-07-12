using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

public sealed class WordTests
{
    [Fact]
    public void FromRaw_ShouldNormalizeWhitespaceAndCase()
    {
        var word = Word.FromRaw("  Hello   World ");

        word.Value.Should().Be("hello world");
    }

    [Fact]
    public void FromRaw_Blank_ShouldThrow()
    {
        Action action = () => Word.FromRaw("   ");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Words_WithSameNormalizedValue_ShouldBeEqual()
    {
        var first = Word.FromRaw("Alpha");
        var second = Word.FromRaw(" alpha ");

        first.Should().Be(second);
    }
}

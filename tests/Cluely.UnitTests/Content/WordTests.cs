using Cluely.Domain.Content;
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
    public void Normalize_ShouldCollapseTabsAndNewlines()
    {
        Word.Normalize("  Hello\t\tWorld\n\nAgain  ").Should().Be("hello world again");
    }

    [Fact]
    public void Normalize_UnicodeCharacters_ShouldPreserveAndLowercase()
    {
        Word.Normalize("  Café\tRésumé  ").Should().Be("café résumé");
    }

    [Fact]
    public void FromRaw_Blank_ShouldThrow()
    {
        Action action = () => Word.FromRaw("   ");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromRaw_ExceedsMaxLength_ShouldThrow()
    {
        var tooLong = new string('a', DictionaryValidation.MaxWordLength + 1);

        Action action = () => Word.FromRaw(tooLong);

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

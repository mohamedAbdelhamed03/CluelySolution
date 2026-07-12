using Cluely.Domain.Content.Errors;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

public sealed class WordSetTests
{
    [Fact]
    public void AddWords_WithinBatchDuplicates_ShouldKeepFirstOccurrence()
    {
        var set = WordSet.Empty();

        var updated = set.AddWords(["Alpha", "  ALPHA\t", "Beta"]);

        updated.Count.Should().Be(2);
        updated.Words.Select(word => word.Value).Should().Equal("alpha", "beta");
    }

    [Fact]
    public void AddWords_ConflictWithExisting_ShouldThrow()
    {
        var set = WordSet.FromWords([Word.FromRaw("alpha")]);

        Action action = () => set.AddWords(["  ALPHA  "]);

        action.Should().Throw<DuplicateWordException>();
    }

    [Fact]
    public void AddWords_BatchOrder_ShouldBeDeterministic()
    {
        var set = WordSet.Empty();

        var updated = set.AddWords(["zebra", "alpha", "beta", "alpha", "zebra"]);

        updated.Words.Select(word => word.Value).Should().Equal("zebra", "alpha", "beta");
    }

    [Fact]
    public void FromWords_WithDuplicates_ShouldThrow()
    {
        Action action = () => WordSet.FromWords([
            Word.FromRaw("alpha"),
            Word.FromRaw("ALPHA")
        ]);

        action.Should().Throw<DuplicateWordException>();
    }

    [Fact]
    public void ReplaceWord_DuplicateTarget_ShouldThrow()
    {
        var set = WordSet.FromWords([
            Word.FromRaw("alpha"),
            Word.FromRaw("beta")
        ]);

        Action action = () => set.ReplaceWord(Word.FromRaw("alpha"), "beta");

        action.Should().Throw<DuplicateWordException>();
    }

    [Fact]
    public void RemoveWord_MissingWord_ShouldThrow()
    {
        var set = WordSet.FromWords([Word.FromRaw("alpha")]);

        Action action = () => set.RemoveWord(Word.FromRaw("missing"));

        action.Should().Throw<WordNotFoundException>();
    }
}

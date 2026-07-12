using Cluely.Domain.Content;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

public sealed class DraftValidationReportTests
{
    [Fact]
    public void FromWordSet_BelowMinimum_ShouldFail()
    {
        var words = WordSet.FromWords(DictionaryTestData.ValidWordBatch(10).Select(Word.FromRaw));

        var report = DraftValidationReport.FromWordSet(words);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(error =>
            error.Contains($"{DictionaryValidation.MinWords}", StringComparison.Ordinal));
    }

    [Fact]
    public void FromWordSet_AtMinimum_ShouldSucceed()
    {
        var words = WordSet.FromWords(DictionaryTestData.ValidWordBatch(DictionaryValidation.MinWords).Select(Word.FromRaw));

        var report = DraftValidationReport.FromWordSet(words);

        report.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FromWordSet_AboveMaximum_ShouldFail()
    {
        var words = WordSet.FromWords(
            DictionaryTestData.ValidWordBatch(DictionaryValidation.MaxWords + 1).Select(Word.FromRaw));

        var report = DraftValidationReport.FromWordSet(words);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(error =>
            error.Contains($"{DictionaryValidation.MaxWords}", StringComparison.Ordinal));
    }
}

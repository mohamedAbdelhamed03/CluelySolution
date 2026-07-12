using Cluely.Application.Content.AddWord;
using Cluely.Application.Content.BulkAddWords;
using Cluely.Application.Content.RemoveWord;
using Cluely.Application.Content.ReplaceWord;
using Cluely.Application.Content.ValidateDraft;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class ContentAuthoringValidatorTests
{
    [Fact]
    public void AddWordValidator_RejectsEmptyWord()
    {
        var validator = new AddWordCommandValidator();

        var result = validator.Validate(new AddWordCommand(Guid.NewGuid(), "", Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemoveWordValidator_RejectsEmptyDictionaryId()
    {
        var validator = new RemoveWordCommandValidator();

        var result = validator.Validate(new RemoveWordCommand(Guid.Empty, "alpha", Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReplaceWordValidator_RejectsEmptyNewWord()
    {
        var validator = new ReplaceWordCommandValidator();

        var result = validator.Validate(new ReplaceWordCommand(
            Guid.NewGuid(),
            "alpha",
            "",
            Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BulkAddWordsValidator_RejectsEmptyList()
    {
        var validator = new BulkAddWordsCommandValidator();

        var result = validator.Validate(new BulkAddWordsCommand(
            Guid.NewGuid(),
            [],
            Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateDraftValidator_AcceptsValidCommand()
    {
        var validator = new ValidateDraftCommandValidator();

        var result = validator.Validate(new ValidateDraftCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}

using Cluely.Application.Content.ArchiveDictionary;
using Cluely.Application.Content.CreateDictionary;
using Cluely.Application.Content.DeleteDictionary;
using Cluely.Application.Content.RenameDictionary;
using Cluely.Application.Content.RestoreDictionary;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class DictionaryLifecycleValidatorTests
{
    [Fact]
    public void CreateDictionaryValidator_Rejects_Empty_Idempotency_Key()
    {
        var validator = new CreateDictionaryCommandValidator();

        var result = validator.Validate(new CreateDictionaryCommand(
            "Title",
            "Description",
            null,
            "en",
            null,
            "user",
            Guid.NewGuid(),
            Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateDictionaryCommand.IdempotencyKey));
    }

    [Fact]
    public void RenameDictionaryValidator_Rejects_Empty_Title()
    {
        var validator = new RenameDictionaryCommandValidator();

        var result = validator.Validate(new RenameDictionaryCommand(
            Guid.NewGuid(),
            "",
            "Description",
            null,
            "en",
            null,
            Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RenameDictionaryCommand.Title));
    }

    [Fact]
    public void ArchiveDictionaryValidator_Rejects_Empty_DictionaryId()
    {
        var validator = new ArchiveDictionaryCommandValidator();

        var result = validator.Validate(new ArchiveDictionaryCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ArchiveDictionaryCommand.DictionaryId));
    }

    [Fact]
    public void DeleteDictionaryValidator_Rejects_Empty_CorrelationId()
    {
        var validator = new DeleteDictionaryCommandValidator();

        var result = validator.Validate(new DeleteDictionaryCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(DeleteDictionaryCommand.CorrelationId));
    }

    [Fact]
    public void RestoreDictionaryValidator_Accepts_Valid_Command()
    {
        var validator = new RestoreDictionaryCommandValidator();

        var result = validator.Validate(new RestoreDictionaryCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}

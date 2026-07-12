using Cluely.Application.Common.Results;
using Cluely.Application.Content.AddWord;
using Cluely.Application.Content.BulkAddWords;
using Cluely.Application.Content.RemoveWord;
using Cluely.Application.Content.ReplaceWord;
using Cluely.Application.Content.ValidateDraft;
using Cluely.Domain.Content;
using Cluely.Domain.Content.Entities;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class ContentAuthoringHandlerTests
{
    private readonly FakeDictionaryRepository _repository = new();
    private readonly FakeDomainEventPublisher _eventPublisher = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();

    public ContentAuthoringHandlerTests()
    {
        _currentUser.UserId = Guid.NewGuid();
    }

    [Fact]
    public async Task AddWord_AddsWordAndPublishesEvent()
    {
        var dictionary = SeedDictionary();
        var handler = new AddWordHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new AddWordCommandValidator());

        var result = await handler.HandleAsync(new AddWordCommand(
            dictionary.Id.Value,
            "Alpha",
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.WordCount.Should().Be(1);
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is WordsChanged);
    }

    [Fact]
    public async Task AddWord_Duplicate_ShouldFailWithoutPersistingOrPublishing()
    {
        var dictionary = SeedDictionary();
        dictionary.AddWords(OwnerId.From(_currentUser.UserId!.Value), ["alpha"]);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new AddWordHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new AddWordCommandValidator());

        var result = await handler.HandleAsync(new AddWordCommand(
            dictionary.Id.Value,
            "ALPHA",
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("DuplicateWordException");
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveWord_RemovesWordAndPublishesEvent()
    {
        var dictionary = SeedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.AddWords(owner, ["alpha", "beta"]);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new RemoveWordHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new RemoveWordCommandValidator());

        var result = await handler.HandleAsync(new RemoveWordCommand(
            dictionary.Id.Value,
            "alpha",
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.WordCount.Should().Be(1);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is WordsChanged);
    }

    [Fact]
    public async Task RemoveWord_MissingWord_ShouldFailWithoutPublishing()
    {
        var dictionary = SeedDictionary();
        var handler = new RemoveWordHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new RemoveWordCommandValidator());

        var result = await handler.HandleAsync(new RemoveWordCommand(
            dictionary.Id.Value,
            "missing",
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("WordNotFoundException");
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ReplaceWord_ReplacesWordAndPublishesEvent()
    {
        var dictionary = SeedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.AddWords(owner, ["alpha"]);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new ReplaceWordHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ReplaceWordCommandValidator());

        var result = await handler.HandleAsync(new ReplaceWordCommand(
            dictionary.Id.Value,
            "alpha",
            "renamed",
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.WordCount.Should().Be(1);
        dictionary.Draft.Words.Words.Single().Value.Should().Be("renamed");
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is WordsChanged);
    }

    [Fact]
    public async Task BulkAddWords_DeduplicatesWithinBatch()
    {
        var dictionary = SeedDictionary();
        var handler = new BulkAddWordsHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new BulkAddWordsCommandValidator());

        var result = await handler.HandleAsync(new BulkAddWordsCommand(
            dictionary.Id.Value,
            ["Alpha", "  alpha\t", "Beta"],
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.WordCount.Should().Be(2);
        result.Value.WordsAdded.Should().Be(2);
    }

    [Fact]
    public async Task ValidateDraft_ExceedingMaxWords_ReturnsInvalidReport()
    {
        var dictionary = SeedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(DictionaryValidation.MaxWords + 1));
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new ValidateDraftHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ValidateDraftCommandValidator());

        var result = await handler.HandleAsync(new ValidateDraftCommand(
            dictionary.Id.Value,
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Errors.Should().ContainSingle(error =>
            error.Contains($"{DictionaryValidation.MaxWords}", StringComparison.Ordinal));
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateDraft_WithTooFewWords_ReturnsInvalidReport()
    {
        var dictionary = SeedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(10));
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new ValidateDraftHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ValidateDraftCommandValidator());

        var result = await handler.HandleAsync(new ValidateDraftCommand(
            dictionary.Id.Value,
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Errors.Should().ContainSingle(error =>
            error.Contains($"{DictionaryValidation.MinWords}", StringComparison.Ordinal));
        dictionary.Draft.State.Should().Be(DraftState.Draft);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateDraft_WithEnoughWords_MarksValidatedWithoutEvents()
    {
        var dictionary = SeedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(25));
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new ValidateDraftHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ValidateDraftCommandValidator());

        var result = await handler.HandleAsync(new ValidateDraftCommand(
            dictionary.Id.Value,
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Errors.Should().BeEmpty();
        dictionary.Draft.State.Should().Be(DraftState.Validated);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthoringHandlers_ReturnUnauthorized_WhenNotAuthenticated()
    {
        _currentUser.UserId = null;
        var handler = new AddWordHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new AddWordCommandValidator());

        var result = await handler.HandleAsync(new AddWordCommand(
            Guid.NewGuid(),
            "alpha",
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("Unauthorized");
    }

    private Dictionary SeedDictionary()
    {
        var dictionary = Dictionary.Create(
            DictionaryId.From(Guid.NewGuid()),
            OwnerId.From(_currentUser.UserId!.Value),
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return dictionary;
    }
}

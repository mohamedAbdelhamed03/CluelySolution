using Cluely.Application.Common.Results;
using Cluely.Application.Content.CancelDeleteDictionary;
using Cluely.Application.Content.ReportDictionary;
using Cluely.Domain.Content;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class PublishingFoundationHandlerTests
{
    private readonly FakeDictionaryRepository _repository = new();
    private readonly FakeDomainEventPublisher _eventPublisher = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();

    public PublishingFoundationHandlerTests()
    {
        _currentUser.UserId = Guid.NewGuid();
    }

    [Fact]
    public async Task CancelDeleteHandler_Restores_Archived_State()
    {
        var dictionary = SeedOwnedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.Archive(owner);
        dictionary.RequestDeletion(owner);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new CancelDeleteDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new CancelDeleteDictionaryCommandValidator());

        var result = await handler.HandleAsync(new CancelDeleteDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        dictionary.State.Should().Be(DictionaryState.Archived);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryDeletionCancelled);
    }

    [Fact]
    public async Task CancelDeleteHandler_OnFailure_ShouldNotPersistOrPublishEvents()
    {
        var dictionary = SeedOwnedDictionary();
        _repository.Seed(dictionary);

        var handler = new CancelDeleteDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new CancelDeleteDictionaryCommandValidator());

        var result = await handler.HandleAsync(new CancelDeleteDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ReportHandler_Reports_Shared_Dictionary()
    {
        var dictionary = SeedOwnedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.SetVisibility(owner, Visibility.Shared);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new ReportDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ReportDictionaryCommandValidator());

        var result = await handler.HandleAsync(new ReportDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryReported);
    }

    [Fact]
    public async Task ReportHandler_DuplicateReports_ShouldEmitSeparateEvents()
    {
        var dictionary = SeedOwnedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.SetVisibility(owner, Visibility.Shared);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new ReportDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ReportDictionaryCommandValidator());

        var first = await handler.HandleAsync(new ReportDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));
        first.IsSuccess.Should().BeTrue();
        _eventPublisher.PublishedEvents.Clear();

        var second = await handler.HandleAsync(new ReportDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        second.IsSuccess.Should().BeTrue();
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryReported);
    }

    [Fact]
    public async Task ReportHandler_OnPrivateDictionary_ShouldNotPersistOrPublishEvents()
    {
        var dictionary = SeedOwnedDictionary();
        _repository.Seed(dictionary);

        var handler = new ReportDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ReportDictionaryCommandValidator());

        var result = await handler.HandleAsync(new ReportDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    private Dictionary SeedOwnedDictionary()
    {
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        var dictionary = Dictionary.Create(
            DictionaryId.From(Guid.NewGuid()),
            owner,
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return dictionary;
    }
}

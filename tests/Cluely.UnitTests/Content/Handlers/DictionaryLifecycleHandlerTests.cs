using Cluely.Application.Common.Results;
using Cluely.Application.Content.ArchiveDictionary;
using Cluely.Application.Content.DeleteDictionary;
using Cluely.Application.Content.RestoreDictionary;
using Cluely.Domain.Content;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class DictionaryLifecycleHandlerTests
{
    private readonly FakeDictionaryRepository _repository = new();
    private readonly FakeDomainEventPublisher _eventPublisher = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();

    public DictionaryLifecycleHandlerTests()
    {
        _currentUser.UserId = Guid.NewGuid();
    }

    [Fact]
    public async Task ArchiveHandler_Archives_Active_Dictionary()
    {
        var dictionary = SeedOwnedDictionary();
        var handler = new ArchiveDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ArchiveDictionaryCommandValidator());

        var result = await handler.HandleAsync(new ArchiveDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryArchived);
    }

    [Fact]
    public async Task ArchiveHandler_Returns_Lifecycle_Error_When_Already_Archived()
    {
        var dictionary = SeedOwnedDictionary();
        dictionary.Archive(OwnerId.From(_currentUser.UserId!.Value));
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new ArchiveDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ArchiveDictionaryCommandValidator());

        var result = await handler.HandleAsync(new ArchiveDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("DictionaryLifecycleException");
        _repository.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task RestoreHandler_Restores_Archived_Dictionary()
    {
        var dictionary = SeedOwnedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.Archive(owner);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        _eventPublisher.PublishedEvents.Clear();

        var handler = new RestoreDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new RestoreDictionaryCommandValidator());

        var result = await handler.HandleAsync(new RestoreDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryRestored);
    }

    [Fact]
    public async Task DeleteHandler_Requests_Deletion_For_Active_Dictionary()
    {
        var dictionary = SeedOwnedDictionary();
        var handler = new DeleteDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new DeleteDictionaryCommandValidator());

        var result = await handler.HandleAsync(new DeleteDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryDeletionRequested);
    }

    [Fact]
    public async Task DeleteHandler_Returns_Lifecycle_Error_When_Deletion_Already_Requested()
    {
        var dictionary = SeedOwnedDictionary();
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        dictionary.RequestDeletion(owner);
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var handler = new DeleteDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new DeleteDictionaryCommandValidator());

        var result = await handler.HandleAsync(new DeleteDictionaryCommand(dictionary.Id.Value, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("DictionaryLifecycleException");
        _repository.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task LifecycleHandlers_Return_NotFound_When_Dictionary_Is_Missing()
    {
        var archiveHandler = new ArchiveDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new ArchiveDictionaryCommandValidator());

        var result = await archiveHandler.HandleAsync(new ArchiveDictionaryCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("DictionaryNotFound");
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

using Cluely.Application.Common.Results;
using Cluely.Application.Content.RenameDictionary;
using Cluely.Domain.Content;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class RenameDictionaryHandlerTests
{
    private readonly FakeDictionaryRepository _repository = new();
    private readonly FakeDomainEventPublisher _eventPublisher = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();
    private readonly RenameDictionaryHandler _handler;

    public RenameDictionaryHandlerTests()
    {
        _currentUser.UserId = Guid.NewGuid();
        _handler = new RenameDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            new RenameDictionaryCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_Renames_Dictionary_And_Publishes_Event()
    {
        var dictionary = SeedOwnedDictionary();

        var result = await _handler.HandleAsync(new RenameDictionaryCommand(
            dictionary.Id.Value,
            "Renamed Dictionary",
            "Updated description",
            ["renamed"],
            "en",
            "US",
            Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Renamed Dictionary");
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryRenamed);
    }

    [Fact]
    public async Task HandleAsync_Returns_NotFound_When_Dictionary_Does_Not_Exist()
    {
        var result = await _handler.HandleAsync(new RenameDictionaryCommand(
            Guid.NewGuid(),
            "Renamed Dictionary",
            "Updated description",
            null,
            "en",
            null,
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("DictionaryNotFound");
        _repository.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Returns_NotOwner_When_Actor_Is_Not_Owner()
    {
        var dictionary = SeedOwnedDictionary(ownerId: Guid.NewGuid());

        var result = await _handler.HandleAsync(new RenameDictionaryCommand(
            dictionary.Id.Value,
            "Renamed Dictionary",
            "Updated description",
            null,
            "en",
            null,
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("NotOwnerException");
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Returns_Validation_Error_When_DictionaryId_Is_Empty()
    {
        var result = await _handler.HandleAsync(new RenameDictionaryCommand(
            Guid.Empty,
            "Renamed Dictionary",
            "Updated description",
            null,
            "en",
            null,
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    private Dictionary SeedOwnedDictionary(Guid? ownerId = null)
    {
        var owner = OwnerId.From(ownerId ?? _currentUser.UserId!.Value);
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

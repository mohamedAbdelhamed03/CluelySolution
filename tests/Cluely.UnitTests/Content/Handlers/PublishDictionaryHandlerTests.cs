using Cluely.Application.Content.PublishDictionary;
using Cluely.Domain.Content;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class PublishDictionaryHandlerTests
{
    private readonly FakeDictionaryRepository _repository = new();
    private readonly FakeContentCommandIdempotencyStore _idempotencyStore = new();
    private readonly FakeDomainEventPublisher _eventPublisher = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();
    private readonly PublishDictionaryCommandValidator _validator = new();

    public PublishDictionaryHandlerTests()
    {
        _currentUser.UserId = Guid.NewGuid();
    }

    [Fact]
    public async Task Publish_ValidOwnedDictionary_ShouldCreateVersionAndPublishEvent()
    {
        var dictionary = SeedOwnedDictionaryWithWords(25);
        var idempotencyKey = Guid.NewGuid();

        var result = await CreateHandler().HandleAsync(
            new PublishDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), idempotencyKey));

        result.IsSuccess.Should().BeTrue();
        result.Value.VersionId.Should().Be(idempotencyKey);
        result.Value.VersionLabel.Should().Be(1);
        result.Value.WordCount.Should().Be(25);
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.OfType<VersionPublished>().Should().ContainSingle();
        dictionary.GetPendingEvents().Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_DuplicateIdempotencyKey_ShouldReturnExistingOutcomeWithoutSecondVersion()
    {
        var dictionary = SeedOwnedDictionaryWithWords(25);
        var idempotencyKey = Guid.NewGuid();
        var handler = CreateHandler();

        var first = await handler.HandleAsync(
            new PublishDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), idempotencyKey));
        var second = await handler.HandleAsync(
            new PublishDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), idempotencyKey));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.Should().BeEquivalentTo(first.Value);
        dictionary.Versions.Should().ContainSingle();
        _repository.UpdateCount.Should().Be(1);
        _eventPublisher.PublishedEvents.OfType<VersionPublished>().Should().ContainSingle();
    }

    [Fact]
    public async Task Publish_WhenUnauthenticated_ShouldFailWithoutPersisting()
    {
        var dictionary = SeedOwnedDictionaryWithWords(25);
        _currentUser.UserId = null;

        var result = await CreateHandler().HandleAsync(
            new PublishDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_WhenDictionaryMissing_ShouldFail()
    {
        var result = await CreateHandler().HandleAsync(
            new PublishDictionaryCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DictionaryNotFound");
        _repository.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Publish_ByNonOwner_ShouldFailWithoutPersistingOrPublishing()
    {
        var otherOwner = OwnerId.From(Guid.NewGuid());
        var dictionary = Dictionary.Create(
            DictionaryId.From(Guid.NewGuid()),
            otherOwner,
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
        dictionary.AddWords(otherOwner, DictionaryTestData.ValidWordBatch(25));
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);

        var result = await CreateHandler().HandleAsync(
            new PublishDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(nameof(Cluely.Domain.Content.Errors.NotOwnerException));
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_WithTooFewWords_ShouldFailWithoutCreatingVersion()
    {
        var dictionary = SeedOwnedDictionaryWithWords(24);

        var result = await CreateHandler().HandleAsync(
            new PublishDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(nameof(Cluely.Domain.Content.Errors.DraftTooSmallException));
        dictionary.Versions.Should().BeEmpty();
        _repository.UpdateCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Publish_WithEmptyDictionaryId_ShouldFailValidation()
    {
        var result = await CreateHandler().HandleAsync(
            new PublishDictionaryCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ValidationFailed");
    }

    [Fact]
    public async Task Publish_TwiceWithDifferentKeys_ShouldCreateTwoVersionsWithIncrementingLabels()
    {
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        var dictionary = SeedOwnedDictionaryWithWords(25);
        var handler = CreateHandler();

        var first = await handler.HandleAsync(
            new PublishDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), Guid.NewGuid()));
        dictionary.AddWords(owner, ["additional"]);
        var second = await handler.HandleAsync(
            new PublishDictionaryCommand(dictionary.Id.Value, Guid.NewGuid(), Guid.NewGuid()));

        first.Value.VersionLabel.Should().Be(1);
        second.Value.VersionLabel.Should().Be(2);
        second.Value.WordCount.Should().Be(26);
        dictionary.Versions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Publish_WhenIdempotencyKeyBoundToAnotherDictionary_ShouldFail()
    {
        var firstDictionary = SeedOwnedDictionaryWithWords(25);
        var secondDictionary = SeedOwnedDictionaryWithWords(25);
        var idempotencyKey = Guid.NewGuid();
        var handler = CreateHandler();

        await handler.HandleAsync(
            new PublishDictionaryCommand(firstDictionary.Id.Value, Guid.NewGuid(), idempotencyKey));

        var result = await handler.HandleAsync(
            new PublishDictionaryCommand(secondDictionary.Id.Value, Guid.NewGuid(), idempotencyKey));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("IdempotencyKeyConflict");
        secondDictionary.Versions.Should().BeEmpty();
    }

    private PublishDictionaryHandler CreateHandler() =>
        new(_repository, _idempotencyStore, _eventPublisher, _currentUser, _validator);

    private Dictionary SeedOwnedDictionaryWithWords(int wordCount)
    {
        var owner = OwnerId.From(_currentUser.UserId!.Value);
        var dictionary = Dictionary.Create(
            DictionaryId.From(Guid.NewGuid()),
            owner,
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
        dictionary.AddWords(owner, DictionaryTestData.ValidWordBatch(wordCount));
        dictionary.ClearPendingEvents();
        _repository.Seed(dictionary);
        return dictionary;
    }
}

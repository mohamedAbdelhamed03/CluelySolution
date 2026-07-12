using Cluely.Application.Common.Results;
using Cluely.Application.Content.CreateDictionary;
using Cluely.Domain.Content;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Handlers;

public sealed class CreateDictionaryHandlerTests
{
    private readonly FakeDictionaryRepository _repository = new();
    private readonly FakeDomainEventPublisher _eventPublisher = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();
    private readonly FakeGuidGenerator _guidGenerator = new();
    private readonly CreateDictionaryHandler _handler;

    public CreateDictionaryHandlerTests()
    {
        _currentUser.UserId = Guid.NewGuid();
        _handler = new CreateDictionaryHandler(
            _repository,
            _eventPublisher,
            _currentUser,
            _guidGenerator,
            new CreateDictionaryCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_Creates_Dictionary_And_Publishes_Events()
    {
        var dictionaryId = Guid.NewGuid();
        _guidGenerator.Enqueue(dictionaryId);

        var command = new CreateDictionaryCommand(
            "Party Words",
            "A fun dictionary",
            ["party"],
            "en",
            "US",
            "user",
            Guid.NewGuid(),
            Guid.NewGuid());

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.DictionaryId.Should().Be(dictionaryId);
        result.Value.Title.Should().Be("Party Words");
        _repository.AddCount.Should().Be(1);
        _eventPublisher.PublishedEvents.Should().ContainSingle(e => e is DictionaryCreated);
    }

    [Fact]
    public async Task HandleAsync_Returns_Existing_Result_When_Idempotency_Key_Matches()
    {
        var ownerId = OwnerId.From(_currentUser.UserId!.Value);
        var dictionaryId = DictionaryId.From(Guid.NewGuid());
        var idempotencyKey = Guid.NewGuid();
        var existing = Dictionary.Create(
            dictionaryId,
            ownerId,
            ContentType.User,
            DictionaryTestData.DefaultMetadata());
        existing.ClearPendingEvents();
        _repository.SeedWithIdempotency(existing, idempotencyKey);

        var command = new CreateDictionaryCommand(
            "Different Title",
            "Different description",
            null,
            "en",
            null,
            "user",
            Guid.NewGuid(),
            idempotencyKey);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.DictionaryId.Should().Be(dictionaryId.Value);
        result.Value.Title.Should().Be("Test Dictionary");
        _repository.AddCount.Should().Be(0);
        _eventPublisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Returns_Unauthorized_When_User_Is_Not_Authenticated()
    {
        _currentUser.UserId = null;

        var result = await _handler.HandleAsync(new CreateDictionaryCommand(
            "Party Words",
            "A fun dictionary",
            null,
            "en",
            null,
            "user",
            Guid.NewGuid(),
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("Unauthorized");
        _repository.AddCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Returns_Validation_Error_When_Title_Is_Missing()
    {
        var result = await _handler.HandleAsync(new CreateDictionaryCommand(
            "",
            "A fun dictionary",
            null,
            "en",
            null,
            "user",
            Guid.NewGuid(),
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        _repository.AddCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Returns_Business_Error_When_Content_Type_Is_Invalid()
    {
        _guidGenerator.Enqueue(Guid.NewGuid());

        var result = await _handler.HandleAsync(new CreateDictionaryCommand(
            "Party Words",
            "A fun dictionary",
            null,
            "en",
            null,
            "invalid",
            Guid.NewGuid(),
            Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("ArgumentException");
        _repository.AddCount.Should().Be(0);
    }
}

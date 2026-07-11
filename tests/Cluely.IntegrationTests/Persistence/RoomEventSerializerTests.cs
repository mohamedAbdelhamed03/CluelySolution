using System.Text.Json;
using Cluely.Domain.Room;
using Cluely.Domain.Room.Events;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Persistence.Mappers;
using Cluely.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Cluely.IntegrationTests.Persistence;

public sealed class RoomEventSerializerTests
{
    [Fact]
    public void Serialize_RoomCreated_ProducesNamedEventType()
    {
        var roomId = RoomId.New();
        var domainEvent = new RoomCreated(roomId, RoomCode.From("TESTCODE"), ParticipantId.New(), "Host");

        var (eventType, eventData) = RoomEventSerializer.Serialize(domainEvent);

        eventType.Should().Be(nameof(RoomCreated));
        eventData.Should().NotBeNullOrWhiteSpace();
        eventData.Should().Contain(roomId.Value.ToString());
    }

    [Fact]
    public void Serialize_BoardGenerated_ProducesValidJson()
    {
        var roomId = RoomId.New();
        var cards = new List<(CardPosition, string, CardOwnership)>
        {
            (CardPosition.From(0), "Apple", CardOwnership.Red),
            (CardPosition.From(1), "Banana", CardOwnership.Blue),
        };

        var domainEvent = new BoardGenerated(roomId, cards, Team.Red);

        var (eventType, eventData) = RoomEventSerializer.Serialize(domainEvent);

        eventType.Should().Be(nameof(BoardGenerated));
        using var document = JsonDocument.Parse(eventData);
        document.RootElement.GetProperty("roomId").GetProperty("value").GetGuid().Should().Be(roomId.Value);
    }

    [Fact]
    public void ToEventEntities_UsesCompileTimeSerializer()
    {
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");

        var entities = room.ToEventEntities(startingSequence: 0, occurredAt: DateTime.UtcNow);

        entities.Should().HaveCount(room.GetPendingEvents().Count);
        entities.Should().OnlyContain(entity => !string.IsNullOrWhiteSpace(entity.EventType));
        entities.Should().OnlyContain(entity => !string.IsNullOrWhiteSpace(entity.EventData));
    }
}

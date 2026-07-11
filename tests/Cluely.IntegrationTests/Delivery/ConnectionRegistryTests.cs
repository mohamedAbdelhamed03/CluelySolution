using Cluely.Infrastructure.Delivery.Connections;
using FluentAssertions;
using Xunit;

namespace Cluely.IntegrationTests.Delivery;

public sealed class ConnectionRegistryTests
{
    [Fact]
    public void Register_AndRemove_TracksConnections()
    {
        var registry = new ConnectionRegistry();
        var roomId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        registry.Register(new RoomConnection("conn-1", roomId, participantId, "Spymaster", "Red"));
        registry.GetRoomConnections(roomId).Should().HaveCount(1);

        registry.Remove("conn-1").Should().BeTrue();
        registry.GetRoomConnections(roomId).Should().BeEmpty();
    }

    [Fact]
    public void Register_MultipleConnectionsForParticipant_ReturnsAll()
    {
        var registry = new ConnectionRegistry();
        var roomId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        registry.Register(new RoomConnection("conn-1", roomId, participantId, "Operative", "Blue"));
        registry.Register(new RoomConnection("conn-2", roomId, participantId, "Operative", "Blue"));

        registry.GetParticipantConnections(roomId, participantId).Should().HaveCount(2);
    }

    [Fact]
    public void GetRoomConnections_IsolatesRooms()
    {
        var registry = new ConnectionRegistry();
        var roomA = Guid.NewGuid();
        var roomB = Guid.NewGuid();

        registry.Register(new RoomConnection("conn-a", roomA, Guid.NewGuid(), "Operative", "Red"));
        registry.Register(new RoomConnection("conn-b", roomB, Guid.NewGuid(), "Operative", "Blue"));

        registry.GetRoomConnections(roomA).Should().HaveCount(1);
        registry.GetRoomConnections(roomB).Should().HaveCount(1);
    }
}

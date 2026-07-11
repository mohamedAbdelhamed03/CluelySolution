using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Delivery.Projections;
using Cluely.Infrastructure.Delivery.Visibility;
using Cluely.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Cluely.IntegrationTests.Delivery;

public sealed class VisibilityFilterTests
{
    private readonly ProjectionBuilder _projectionBuilder = new();
    private readonly VisibilityFilter _visibilityFilter = new();

    [Fact]
    public void Filter_Spymaster_SeesUnrevealedOwnership()
    {
        var room = RoomTestData.CreateRoomWithMatchStarted();
        var internalProjection = _projectionBuilder.Build(room);

        var filtered = _visibilityFilter.Filter(internalProjection, Role.Spymaster, Team.Red);

        filtered.Board.Should().NotBeNull();
        filtered.Board!.Cards.Should().OnlyContain(card => card.Ownership != null);
        filtered.Board.Cards.Should().Contain(card => card.IsRevealed == false && card.Ownership != null);
    }

    [Fact]
    public void Filter_Operative_HidesUnrevealedOwnership()
    {
        var room = RoomTestData.CreateRoomWithMatchStarted();
        var internalProjection = _projectionBuilder.Build(room);

        var filtered = _visibilityFilter.Filter(internalProjection, Role.Operative, Team.Red);

        filtered.Board.Should().NotBeNull();
        filtered.Board!.Cards.Where(card => !card.IsRevealed)
            .Should()
            .OnlyContain(card => card.Ownership == null);
    }

    [Fact]
    public void Filter_OperativeOnBlueTeam_StillHidesKey()
    {
        var room = RoomTestData.CreateRoomWithMatchStarted();
        var internalProjection = _projectionBuilder.Build(room);

        var filtered = _visibilityFilter.Filter(internalProjection, Role.Operative, Team.Blue);

        filtered.Board!.Cards.Where(card => !card.IsRevealed)
            .Should()
            .OnlyContain(card => card.Ownership == null);
    }
}

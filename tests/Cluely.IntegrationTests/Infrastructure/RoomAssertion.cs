using Cluely.Domain.Room;
using Cluely.Domain.Room.Entities;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Persistence.RoomCustody;
using FluentAssertions;

namespace Cluely.IntegrationTests.Infrastructure;

internal static class RoomAssertion
{
    public static void ShouldBeEquivalentTo(this Room actual, Room expected)
    {
        actual.Id.Should().Be(expected.Id);
        actual.Code.Should().Be(expected.Code);
        actual.Version.Should().Be(expected.Version);
        actual.State.Should().Be(expected.State);
        actual.StartingTeam.Should().Be(expected.StartingTeam);
        actual.WinningTeam.Should().Be(expected.WinningTeam);
        actual.Dictionary.Should().Be(expected.Dictionary);
        actual.Participants.Should().HaveCount(expected.Participants.Count);

        foreach (var expectedParticipant in expected.Participants)
        {
            var actualParticipant = actual.Participants.Single(p => p.Id == expectedParticipant.Id);
            actualParticipant.Nickname.Should().Be(expectedParticipant.Nickname);
            actualParticipant.Team.Should().Be(expectedParticipant.Team);
            actualParticipant.Role.Should().Be(expectedParticipant.Role);
            actualParticipant.IsHost.Should().Be(expectedParticipant.IsHost);
        }

        if (expected.Board is null)
        {
            actual.Board.Should().BeNull();
        }
        else
        {
            actual.Board.Should().NotBeNull();
            actual.Board!.RedRemaining.Should().Be(expected.Board!.RedRemaining);
            actual.Board.BlueRemaining.Should().Be(expected.Board.BlueRemaining);
            actual.Board.Cards.Should().HaveCount(expected.Board.Cards.Count);

            foreach (var expectedCard in expected.Board.Cards)
            {
                var actualCard = actual.Board.Cards.Single(c => c.Position == expectedCard.Position);
                actualCard.Word.Should().Be(expectedCard.Word);
                actualCard.Ownership.Should().Be(expectedCard.Ownership);
                actualCard.IsRevealed.Should().Be(expectedCard.IsRevealed);
            }
        }

        if (expected.CurrentTurn is null)
        {
            actual.CurrentTurn.Should().BeNull();
        }
        else
        {
            actual.CurrentTurn.Should().NotBeNull();
            actual.CurrentTurn!.Number.Should().Be(expected.CurrentTurn!.Number);
            actual.CurrentTurn.ActiveTeam.Should().Be(expected.CurrentTurn.ActiveTeam);
            actual.CurrentTurn.GuessesUsed.Should().Be(expected.CurrentTurn.GuessesUsed);
            actual.CurrentTurn.Clue?.Word.Should().Be(expected.CurrentTurn.Clue?.Word);
            actual.CurrentTurn.Clue?.Number.Should().Be(expected.CurrentTurn.Clue?.Number);
        }
    }
}

internal static class RoomTestData
{
    public static Room CreateLobbyRoom(string? code = null)
    {
        var roomCode = RoomCode.From(code ?? $"R{Guid.NewGuid():N}"[..8].ToUpperInvariant());
        var room = Room.Create(RoomId.New(), roomCode, "HostPlayer");
        room.ClearPendingEvents();
        return room;
    }

    public static Room CreateRoomWithMatchStarted()
    {
        var room = CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Player1");
        room.Join(ParticipantId.New(), "Player2");
        room.Join(ParticipantId.New(), "Player3");

        var host = room.Participants.Single(p => p.IsHost);
        room.AssignTeam(host.Id, Team.Red);
        room.AssignRole(host.Id, Role.Spymaster);

        var player1 = room.Participants.Single(p => p.Nickname == "Player1");
        room.AssignTeam(player1.Id, Team.Red);
        room.AssignRole(player1.Id, Role.Operative);

        var player2 = room.Participants.Single(p => p.Nickname == "Player2");
        room.AssignTeam(player2.Id, Team.Blue);
        room.AssignRole(player2.Id, Role.Spymaster);

        var player3 = room.Participants.Single(p => p.Nickname == "Player3");
        room.AssignTeam(player3.Id, Team.Blue);
        room.AssignRole(player3.Id, Role.Operative);

        room.SelectDictionary(DictionaryReference.Create(RegionCode.From("en-US"), ContentVersion.From("1.0.0")));
        room.StartMatch(room.HostId);
        return room;
    }
}

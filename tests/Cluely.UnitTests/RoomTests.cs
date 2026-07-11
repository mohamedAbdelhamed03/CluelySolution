using Cluely.Domain.Room;
using Cluely.Domain.Room.Entities;
using Cluely.Domain.Room.Errors;
using Cluely.Domain.Room.Events;
using Cluely.Domain.Room.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests;

public class RoomTests
{
    [Fact]
    public void CreateRoom_ShouldSucceed()
    {
        var roomId = RoomId.New();
        var roomCode = RoomCode.From("ABCD123");
        var hostNickname = "HostPlayer";

        var room = Room.Create(roomId, roomCode, hostNickname);

        room.State.Should().Be(RoomState.Lobby);
        room.Participants.Count.Should().Be(1);
        room.Participants[0].IsHost.Should().BeTrue();
        room.Participants[0].Nickname.Should().Be(hostNickname);
    }

    [Fact]
    public void JoinRoom_ShouldAddParticipant()
    {
        var room = CreateTestRoom();
        var participantId = ParticipantId.New();

        room.Join(participantId, "Player1");

        room.Participants.Should().HaveCount(2);
        room.Participants.Any(p => p.Id == participantId).Should().BeTrue();
        room.GetPendingEvents().OfType<PlayerJoined>().Should().NotBeEmpty();
    }

    [Fact]
    public void JoinRoom_DuplicateNickname_ShouldThrow()
    {
        var room = CreateTestRoom();
        var participantId1 = ParticipantId.New();

        room.Join(participantId1, "Player1");

        var participantId2 = ParticipantId.New();
        Action action = () => room.Join(participantId2, "Player1");

        action.Should().Throw<DuplicateNicknameException>();
    }

    [Fact]
    public void AssignTeam_ShouldSucceed()
    {
        var room = CreateTestRoom();
        var participantId = ParticipantId.New();
        room.Join(participantId, "Player1");
        room.ClearPendingEvents();

        room.AssignTeam(participantId, Team.Red);

        var participant = room.Participants.Single(p => p.Id == participantId);
        participant.Team.Should().Be(Team.Red);
        room.GetPendingEvents().OfType<TeamChanged>().Should().NotBeEmpty();
    }

    [Fact]
    public void AssignRole_Spymaster_ShouldSucceed()
    {
        var room = CreateTestRoom();
        var participantId = ParticipantId.New();
        room.Join(participantId, "Player1");
        room.AssignTeam(participantId, Team.Red);
        room.ClearPendingEvents();

        room.AssignRole(participantId, Role.Spymaster);

        var participant = room.Participants.Single(p => p.Id == participantId);
        participant.Role.Should().Be(Role.Spymaster);
        room.GetPendingEvents().OfType<RoleChanged>().Should().NotBeEmpty();
    }

    [Fact]
    public void AssignRole_TwoSpymasters_ShouldThrow()
    {
        var room = CreateTestRoom();
        var participantId1 = ParticipantId.New();
        room.Join(participantId1, "Player1");
        room.AssignTeam(participantId1, Team.Red);
        room.AssignRole(participantId1, Role.Spymaster);

        var participantId2 = ParticipantId.New();
        room.Join(participantId2, "Player2");
        room.AssignTeam(participantId2, Team.Red);
        room.ClearPendingEvents();

        Action action = () => room.AssignRole(participantId2, Role.Spymaster);

        action.Should().Throw<RoleAlreadyTakenException>();
    }

    [Fact]
    public void StartMatch_ShouldSucceed()
    {
        var room = CreateTestRoomWithPlayers();
        room.SelectDictionary(DictionaryReference.Create(RegionCode.From("en-US"), ContentVersion.From("1.0.0")));
        room.ClearPendingEvents();

        room.StartMatch(room.HostId);

        room.State.Should().Be(RoomState.InProgress);
        room.Board.Should().NotBeNull();
        room.CurrentTurn.Should().NotBeNull();
        room.GetPendingEvents().OfType<GameStarted>().Should().NotBeEmpty();
    }

    [Fact]
    public void StartMatch_NotHost_ShouldThrow()
    {
        var room = CreateTestRoomWithPlayers();
        room.SelectDictionary(DictionaryReference.Create(RegionCode.From("en-US"), ContentVersion.From("1.0.0")));
        var nonHostParticipantId = room.Participants.First(p => !p.IsHost).Id;

        Action action = () => room.StartMatch(nonHostParticipantId);

        action.Should().Throw<NotRoomHostException>();
    }

    [Fact]
    public void SubmitClue_ShouldSucceed()
    {
        var room = CreateTestRoomWithPlayers();
        room.SelectDictionary(DictionaryReference.Create(RegionCode.From("en-US"), ContentVersion.From("1.0.0")));
        room.StartMatch(room.HostId);
        var spymaster = room.Participants.First(p => p.Team == room.CurrentTurn!.ActiveTeam && p.Role == Role.Spymaster);
        room.ClearPendingEvents();

        room.SubmitClue(spymaster.Id, Clue.Create("Test", 2));

        room.CurrentTurn!.Clue.Should().NotBeNull();
        room.GetPendingEvents().OfType<ClueSubmitted>().Should().NotBeEmpty();
    }

    [Fact]
    public void SubmitClue_NotSpymaster_ShouldThrow()
    {
        var room = CreateTestRoomWithPlayers();
        room.SelectDictionary(DictionaryReference.Create(RegionCode.From("en-US"), ContentVersion.From("1.0.0")));
        room.StartMatch(room.HostId);
        var operative = room.Participants.First(p => p.Team == room.CurrentTurn!.ActiveTeam && p.Role == Role.Operative);
        room.ClearPendingEvents();

        Action action = () => room.SubmitClue(operative.Id, Clue.Create("Test", 2));

        action.Should().Throw<NotSpymasterException>();
    }

    [Fact]
    public void SubmitGuess_ShouldSucceed()
    {
        var room = CreateTestRoomWithPlayers();
        room.SelectDictionary(DictionaryReference.Create(RegionCode.From("en-US"), ContentVersion.From("1.0.0")));
        room.StartMatch(room.HostId);
        var spymaster = room.Participants.First(p => p.Team == room.CurrentTurn!.ActiveTeam && p.Role == Role.Spymaster);
        room.SubmitClue(spymaster.Id, Clue.Create("Test", 2));
        var operative = room.Participants.First(p => p.Team == room.CurrentTurn!.ActiveTeam && p.Role == Role.Operative);
        room.ClearPendingEvents();

        room.SubmitGuess(operative.Id, CardPosition.From(0));

        room.Board!.Cards[0].IsRevealed.Should().BeTrue();
        room.GetPendingEvents().OfType<CardRevealed>().Should().NotBeEmpty();
    }

    private Room CreateTestRoom()
    {
        var roomId = RoomId.New();
        var roomCode = RoomCode.From("ABCD123");
        var hostNickname = "HostPlayer";
        var room = Room.Create(roomId, roomCode, hostNickname);
        room.ClearPendingEvents();
        return room;
    }

    private Room CreateTestRoomWithPlayers()
    {
        var room = CreateTestRoom();
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

        room.ClearPendingEvents();
        return room;
    }
}

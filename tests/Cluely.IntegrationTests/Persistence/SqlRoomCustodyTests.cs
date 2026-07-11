using Cluely.Domain.Room;
using Cluely.Domain.Room.Entities;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Persistence.Exceptions;
using Cluely.Infrastructure.Persistence.Models;
using Cluely.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cluely.IntegrationTests.Persistence;

[Collection(nameof(SqlServerTestCollection))]
public sealed class SqlRoomCustodyTests
{
    private readonly SqlServerTestDatabase _database;

    public SqlRoomCustodyTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task SaveAsync_NewRoom_PersistsSnapshotAndEventTail()
    {
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");

        await using var context = _database.CreateCustodyContext();
        await context.Custody.SaveAsync(room);

        var snapshot = await context.DbContext.RoomSnapshots.SingleAsync(rs => rs.RoomId == room.Id.Value);
        snapshot.RoomCode.Should().Be(room.Code.Value);
        snapshot.Version.Should().Be(room.Version.Value);
        snapshot.SerializedState.Should().NotBeNullOrWhiteSpace();

        var events = await context.DbContext.RoomEvents.Where(e => e.RoomId == room.Id.Value).ToListAsync();
        events.Should().HaveCount(1);
        events[0].AggregateVersion.Should().Be(room.Version.Value);
        events[0].Sequence.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_AfterSave_RecoversEquivalentAggregate()
    {
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");

        await using (var saveContext = _database.CreateCustodyContext())
        {
            await saveContext.Custody.SaveAsync(room);
        }

        await using var recoverContext = _database.CreateCustodyContext();
        var recovered = await recoverContext.Custody.GetAsync(room.Id);

        recovered.Should().NotBeNull();
        var recoveredRoom = recovered!;
        recoveredRoom.ShouldBeEquivalentTo(room);
        recoveredRoom.GetPendingEvents().Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCodeAsync_AfterSave_RecoversEquivalentAggregate()
    {
        var roomCode = RoomCode.From($"C{Guid.NewGuid():N}"[..8].ToUpperInvariant());
        var room = Room.Create(RoomId.New(), roomCode, "HostPlayer");
        room.Join(ParticipantId.New(), "Guest");

        await using (var saveContext = _database.CreateCustodyContext())
        {
            await saveContext.Custody.SaveAsync(room);
        }

        await using var recoverContext = _database.CreateCustodyContext();
        var recovered = await recoverContext.Custody.GetByCodeAsync(room.Code);

        recovered.Should().NotBeNull();
        recovered!.ShouldBeEquivalentTo(room);
    }

    [Fact]
    public async Task SaveAsync_MultipleCommits_AppendsEventTailAndPreservesVersion()
    {
        var room = RoomTestData.CreateLobbyRoom();

        await using (var context = _database.CreateCustodyContext())
        {
            room.Join(ParticipantId.New(), "Guest");
            await context.Custody.SaveAsync(room);
            room.ClearPendingEvents();

            room.AssignTeam(room.Participants.First(p => !p.IsHost).Id, Team.Red);
            await context.Custody.SaveAsync(room);
            room.ClearPendingEvents();
        }

        await using var recoverContext = _database.CreateCustodyContext();
        var recovered = await recoverContext.Custody.GetAsync(room.Id);
        recovered.Should().NotBeNull();
        recovered!.Version.Value.Should().Be(2);

        var events = await recoverContext.DbContext.RoomEvents
            .Where(e => e.RoomId == room.Id.Value)
            .OrderBy(e => e.Sequence)
            .ToListAsync();

        events.Should().HaveCount(2);
        events[0].Sequence.Should().Be(1);
        events[1].Sequence.Should().Be(2);
        events.Select(e => e.AggregateVersion).Should().BeEquivalentTo([1, 2], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task SaveAsync_MatchStartedState_RecoversAfterRestart()
    {
        var room = RoomTestData.CreateRoomWithMatchStarted();
        var spymaster = room.Participants.First(p => p.Team == room.CurrentTurn!.ActiveTeam && p.Role == Role.Spymaster);
        room.SubmitClue(spymaster.Id, Clue.Create("Ocean", 2));

        await using (var saveContext = _database.CreateCustodyContext())
        {
            await saveContext.Custody.SaveAsync(room);
        }

        await using var recoverContext = _database.CreateCustodyContext();
        var recovered = await recoverContext.Custody.GetAsync(room.Id);

        recovered.Should().NotBeNull();
        var recoveredRoom = recovered!;
        recoveredRoom.ShouldBeEquivalentTo(room);
        recoveredRoom.State.Should().Be(RoomState.InProgress);
        recoveredRoom.Board.Should().NotBeNull();
        recoveredRoom.CurrentTurn.Should().NotBeNull();
        recoveredRoom.CurrentTurn!.Clue.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveAsync_NoPendingEvents_IsNoOp()
    {
        var room = RoomTestData.CreateLobbyRoom();

        await using var context = _database.CreateCustodyContext();
        var countBefore = await context.DbContext.RoomSnapshots.CountAsync();
        await context.Custody.SaveAsync(room);
        var countAfter = await context.DbContext.RoomSnapshots.CountAsync();
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task SaveAsync_DuplicatePersistence_ThrowsRoomCustodyException()
    {
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");

        await using var context = _database.CreateCustodyContext();
        await context.Custody.SaveAsync(room);

        var act = async () => await context.Custody.SaveAsync(room);
        await act.Should().ThrowAsync<RoomCustodyException>();
    }

    [Fact]
    public async Task SaveAsync_VersionConflict_ThrowsRoomCustodyException()
    {
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");

        await using (var context = _database.CreateCustodyContext())
        {
            await context.Custody.SaveAsync(room);
        }

        await using var staleContext = _database.CreateCustodyContext();
        var staleRoom = await staleContext.Custody.GetAsync(room.Id);
        staleRoom.Should().NotBeNull();
        staleRoom!.Join(ParticipantId.New(), "AnotherGuest");

        room.ClearPendingEvents();
        room.AssignTeam(room.Participants.First(p => !p.IsHost).Id, Team.Red);

        await using (var winningContext = _database.CreateCustodyContext())
        {
            await winningContext.Custody.SaveAsync(room);
        }

        var act = async () => await staleContext.Custody.SaveAsync(staleRoom);
        await act.Should().ThrowAsync<RoomCustodyException>();
    }

    [Fact]
    public async Task SaveAsync_TransactionRollback_DoesNotPersistPartialState()
    {
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");

        await using var context = _database.CreateCustodyContext();
        await context.Custody.SaveAsync(room);
        room.ClearPendingEvents();

        var conflictingRoom = Room.Create(RoomId.New(), room.Code, "OtherHost");
        conflictingRoom.Join(ParticipantId.New(), "OtherGuest");

        var act = async () => await context.Custody.SaveAsync(conflictingRoom);
        await act.Should().ThrowAsync<RoomCustodyException>();

        var roomSnapshotExists = await context.DbContext.RoomSnapshots.AnyAsync(rs => rs.RoomId == room.Id.Value);
        roomSnapshotExists.Should().BeTrue();

        var conflictingSnapshotExists = await context.DbContext.RoomSnapshots.AnyAsync(rs => rs.RoomId == conflictingRoom.Id.Value);
        conflictingSnapshotExists.Should().BeFalse();

        var conflictingEventCount = await context.DbContext.RoomEvents.CountAsync(e => e.RoomId == conflictingRoom.Id.Value);
        conflictingEventCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_CorruptedSnapshot_ThrowsRoomCustodyException()
    {
        var room = RoomTestData.CreateLobbyRoom();

        await using (var saveContext = _database.CreateCustodyContext())
        {
            room.Join(ParticipantId.New(), "Guest");
            await saveContext.Custody.SaveAsync(room);
        }

        await using var corruptContext = _database.CreateCustodyContext();
        var snapshot = await corruptContext.DbContext.RoomSnapshots.SingleAsync(rs => rs.RoomId == room.Id.Value);
        snapshot.SerializedState = "{not-valid-json";
        await corruptContext.DbContext.SaveChangesAsync();

        var act = async () => await corruptContext.Custody.GetAsync(room.Id);
        await act.Should().ThrowAsync<RoomCustodyException>();
    }

    [Fact]
    public async Task GetAsync_TailAheadOfSnapshot_ThrowsRoomCustodyException()
    {
        var room = RoomTestData.CreateLobbyRoom();

        await using (var saveContext = _database.CreateCustodyContext())
        {
            room.Join(ParticipantId.New(), "Guest");
            await saveContext.Custody.SaveAsync(room);
        }

        await using var corruptContext = _database.CreateCustodyContext();
        var snapshot = await corruptContext.DbContext.RoomSnapshots.SingleAsync(rs => rs.RoomId == room.Id.Value);
        snapshot.Version = 0;
        corruptContext.DbContext.RoomEvents.Add(new RoomEvent
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id.Value,
            Sequence = 99,
            AggregateVersion = 5,
            EventType = "PlayerJoined",
            EventData = "{}",
            OccurredAt = DateTime.UtcNow,
        });
        await corruptContext.DbContext.SaveChangesAsync();

        var act = async () => await corruptContext.Custody.GetAsync(room.Id);
        await act.Should().ThrowAsync<RoomCustodyException>()
            .WithMessage("*committed event(s) after snapshot version*");
    }

    [Fact]
    public async Task SaveAsync_StartMatch_AppendsMultipleEventsAtSameVersion()
    {
        var room = RoomTestData.CreateLobbyRoom();
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
        room.ClearPendingEvents();

        room.StartMatch(room.HostId);

        await using var context = _database.CreateCustodyContext();
        await context.Custody.SaveAsync(room);

        var events = await context.DbContext.RoomEvents
            .Where(e => e.RoomId == room.Id.Value && e.AggregateVersion == room.Version.Value)
            .ToListAsync();

        events.Count.Should().BeGreaterThan(1);
        events.Select(e => e.Sequence).Should().OnlyHaveUniqueItems();
    }
}

[CollectionDefinition(nameof(SqlServerTestCollection))]
public sealed class SqlServerTestCollection : ICollectionFixture<SqlServerTestDatabase>
{
}

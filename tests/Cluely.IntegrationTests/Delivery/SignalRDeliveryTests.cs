using Cluely.Application.Common.Ports;
using Cluely.Domain.Room;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Delivery.Connections;
using Cluely.Infrastructure.Delivery.Contracts;
using Cluely.Infrastructure.Delivery.Dispatch;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cluely.IntegrationTests.Delivery;

[Collection(nameof(SqlServerTestCollection))]
public sealed class SignalRDeliveryTests
{
    private readonly SqlServerTestDatabase _database;

    public SignalRDeliveryTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task JoinRoom_SendsSnapshot_WithCommittedVersion()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");
        await SaveRoomAsync(host, room);

        var hostParticipant = room.Participants.Single(p => p.IsHost);
        await using var connection = await ConnectBoundAsync(host, room.Id.Value, hostParticipant.Id.Value);

        DeliveryEnvelope<RoomProjectionDto>? snapshot = null;
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(DeliveryHubMethods.ReceiveSnapshot, envelope =>
        {
            snapshot = envelope;
        });

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);

        await SignalRTestSupport.WaitForAsync(() => snapshot is not null);
        snapshot!.RoomId.Should().Be(room.Id.Value);
        snapshot.AggregateVersion.Should().Be(room.Version.Value);
        snapshot.IsSnapshot.Should().BeTrue();
        snapshot.Projection.Participants.Should().HaveCount(2);

        await connection.StopAsync();
    }

    [Fact]
    public async Task JoinRoom_SpymasterSnapshot_IncludesHiddenOwnership()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateRoomWithMatchStarted();
        var spymaster = room.Participants.First(p => p.Role == Role.Spymaster);
        await SaveRoomAsync(host, room);

        await using var connection = await ConnectBoundAsync(host, room.Id.Value, spymaster.Id.Value);
        DeliveryEnvelope<RoomProjectionDto>? snapshot = null;
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(DeliveryHubMethods.ReceiveSnapshot, envelope =>
        {
            snapshot = envelope;
        });

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => snapshot is not null);

        snapshot!.Projection.Board.Should().NotBeNull();
        snapshot.Projection.Board!.Cards.Should().Contain(card => !card.IsRevealed && card.Ownership != null);

        await connection.StopAsync();
    }

    [Fact]
    public async Task JoinRoom_OperativeSnapshot_HidesUnrevealedOwnership()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateRoomWithMatchStarted();
        var operative = room.Participants.First(p => p.Role == Role.Operative);
        await SaveRoomAsync(host, room);

        await using var connection = await ConnectBoundAsync(host, room.Id.Value, operative.Id.Value);
        DeliveryEnvelope<RoomProjectionDto>? snapshot = null;
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(DeliveryHubMethods.ReceiveSnapshot, envelope =>
        {
            snapshot = envelope;
        });

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => snapshot is not null);

        snapshot!.Projection.Board.Should().NotBeNull();
        snapshot.Projection.Board!.Cards.Where(card => !card.IsRevealed)
            .Should()
            .OnlyContain(card => card.Ownership == null);

        await connection.StopAsync();
    }

    [Fact]
    public async Task PublishCommittedChange_SendsIncrementalUpdate_WithNewerVersion()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");
        await SaveRoomAsync(host, room);

        var hostParticipant = room.Participants.Single(p => p.IsHost);
        await using var connection = await ConnectBoundAsync(host, room.Id.Value, hostParticipant.Id.Value);

        DeliveryEnvelope<RoomProjectionDto>? snapshot = null;
        DeliveryEnvelope<RoomProjectionDto>? update = null;
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(DeliveryHubMethods.ReceiveSnapshot, envelope => snapshot = envelope);
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(DeliveryHubMethods.ReceiveUpdate, envelope => update = envelope);

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => snapshot is not null);

        room.ClearPendingEvents();
        var guest = room.Participants.First(p => !p.IsHost);
        room.AssignTeam(guest.Id, Team.Red);

        await using var scope = host.Services.CreateAsyncScope();
        var custody = scope.ServiceProvider.GetRequiredService<IRoomCustody>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        await custody.SaveAsync(room);
        await publisher.PublishAsync(room.GetPendingEvents());

        await SignalRTestSupport.WaitForAsync(() => update is not null);
        update!.IsSnapshot.Should().BeFalse();
        update.AggregateVersion.Should().BeGreaterThan(snapshot!.AggregateVersion);

        await connection.StopAsync();
    }

    [Fact]
    public async Task JoinRoom_UnknownParticipant_SendsDeliveryError()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = Room.Create(RoomId.New(), RoomCode.From($"R{Guid.NewGuid():N}"[..8].ToUpperInvariant()), "Host");
        await SaveRoomAsync(host, room);

        await using var connection = await ConnectBoundAsync(host, room.Id.Value, Guid.NewGuid());
        string? error = null;
        connection.On<string>(DeliveryHubMethods.DeliveryError, code => error = code);

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => error is not null);

        error.Should().Be("ParticipantNotFound");
        await connection.StopAsync();
    }

    [Fact]
    public async Task Reconnect_ReceivesFreshSnapshot()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");
        await SaveRoomAsync(host, room);

        var hostParticipant = room.Participants.Single(p => p.IsHost);
        var token = await SignalRTestSupport.BindParticipantAndGetAccessTokenAsync(
            host,
            room.Id.Value,
            hostParticipant.Id.Value);

        await using (var firstConnection = SignalRTestSupport.CreateConnection(host, token))
        {
            DeliveryEnvelope<RoomProjectionDto>? firstSnapshot = null;
            firstConnection.On<DeliveryEnvelope<RoomProjectionDto>>(
                DeliveryHubMethods.ReceiveSnapshot,
                envelope => firstSnapshot = envelope);

            await firstConnection.StartAsync();
            await firstConnection.InvokeAsync("JoinRoom", room.Id.Value);
            await SignalRTestSupport.WaitForAsync(() => firstSnapshot is not null);
            await firstConnection.StopAsync();
        }

        await using var secondConnection = SignalRTestSupport.CreateConnection(host, token);
        DeliveryEnvelope<RoomProjectionDto>? reconnectSnapshot = null;
        secondConnection.On<DeliveryEnvelope<RoomProjectionDto>>(
            DeliveryHubMethods.ReceiveSnapshot,
            envelope => reconnectSnapshot = envelope);

        await secondConnection.StartAsync();
        await secondConnection.InvokeAsync("JoinRoom", room.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => reconnectSnapshot is not null);

        reconnectSnapshot!.IsSnapshot.Should().BeTrue();
        reconnectSnapshot.AggregateVersion.Should().Be(room.Version.Value);
        reconnectSnapshot.RoomId.Should().Be(room.Id.Value);

        await secondConnection.StopAsync();
    }

    [Fact]
    public async Task BroadcastUpdate_IsolatesRooms()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var roomA = RoomTestData.CreateLobbyRoom();
        roomA.Join(ParticipantId.New(), "GuestA");
        var roomB = RoomTestData.CreateLobbyRoom();
        roomB.Join(ParticipantId.New(), "GuestB");

        await SaveRoomAsync(host, roomA);
        await SaveRoomAsync(host, roomB);

        var hostA = roomA.Participants.Single(p => p.IsHost);
        var hostB = roomB.Participants.Single(p => p.IsHost);

        await using var connectionA = await ConnectBoundAsync(host, roomA.Id.Value, hostA.Id.Value);
        await using var connectionB = await ConnectBoundAsync(host, roomB.Id.Value, hostB.Id.Value);

        DeliveryEnvelope<RoomProjectionDto>? snapshotA = null;
        DeliveryEnvelope<RoomProjectionDto>? updateB = null;

        connectionA.On<DeliveryEnvelope<RoomProjectionDto>>(
            DeliveryHubMethods.ReceiveSnapshot,
            envelope => snapshotA = envelope);
        connectionB.On<DeliveryEnvelope<RoomProjectionDto>>(
            DeliveryHubMethods.ReceiveUpdate,
            envelope => updateB = envelope);

        await connectionA.StartAsync();
        await connectionB.StartAsync();
        await connectionA.InvokeAsync("JoinRoom", roomA.Id.Value);
        await connectionB.InvokeAsync("JoinRoom", roomB.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => snapshotA is not null);

        roomA.ClearPendingEvents();
        var guestA = roomA.Participants.First(p => !p.IsHost);
        roomA.AssignTeam(guestA.Id, Team.Red);

        await using var scope = host.Services.CreateAsyncScope();
        var custody = scope.ServiceProvider.GetRequiredService<IRoomCustody>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        await custody.SaveAsync(roomA);
        await publisher.PublishAsync(roomA.GetPendingEvents());

        await Task.Delay(300);
        updateB.Should().BeNull();

        await connectionA.StopAsync();
        await connectionB.StopAsync();
    }

    [Fact]
    public async Task Disconnect_RemovesConnection_FromRegistry()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");
        await SaveRoomAsync(host, room);

        var hostParticipant = room.Participants.Single(p => p.IsHost);
        var connection = await ConnectBoundAsync(host, room.Id.Value, hostParticipant.Id.Value);
        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);

        await connection.StopAsync();
        await connection.DisposeAsync();

        using var scope = host.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IConnectionRegistry>();
        await SignalRTestSupport.WaitForAsync(() => registry.GetRoomConnections(room.Id.Value).Count == 0);
    }

    private static async Task<HubConnection> ConnectBoundAsync(
        SignalRTestHost host,
        Guid roomId,
        Guid participantId)
    {
        var token = await SignalRTestSupport.BindParticipantAndGetAccessTokenAsync(host, roomId, participantId);
        return SignalRTestSupport.CreateConnection(host, token);
    }

    private static async Task SaveRoomAsync(SignalRTestHost host, Room room)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var custody = scope.ServiceProvider.GetRequiredService<IRoomCustody>();
        await custody.SaveAsync(room);
        room.ClearPendingEvents();
    }
}

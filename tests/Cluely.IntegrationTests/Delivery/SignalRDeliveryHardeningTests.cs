using Cluely.Application.Common.Ports;
using Cluely.Domain.Room;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Delivery.Connections;
using Cluely.Infrastructure.Delivery.Contracts;
using Cluely.Infrastructure.Delivery.Dispatch;
using Cluely.Infrastructure.Delivery.Projections;
using Cluely.Infrastructure.Delivery.Visibility;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cluely.IntegrationTests.Delivery;

[Collection(nameof(SqlServerTestCollection))]
public sealed class SignalRDeliveryHardeningTests
{
    private readonly SqlServerTestDatabase _database;

    public SignalRDeliveryHardeningTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task JoinRoom_RoomNotFound_SendsDeliveryError()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var missingRoomId = Guid.NewGuid();
        await using var connection = await ConnectBoundAsync(host, missingRoomId, Guid.NewGuid());

        string? error = null;
        connection.On<string>(DeliveryHubMethods.DeliveryError, code => error = code);

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", missingRoomId);
        await SignalRTestSupport.WaitForAsync(() => error is not null);

        error.Should().Be("RoomNotFound");
        await connection.StopAsync();
    }

    [Fact]
    public async Task LeaveRoom_RemovesConnection_FromRegistry()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");
        await SaveRoomAsync(host, room);

        var hostParticipant = room.Participants.Single(p => p.IsHost);
        await using var connection = await ConnectBoundAsync(host, room.Id.Value, hostParticipant.Id.Value);
        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);
        await connection.InvokeAsync("LeaveRoom");

        using var scope = host.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IConnectionRegistry>();
        registry.GetRoomConnections(room.Id.Value).Should().BeEmpty();

        await connection.StopAsync();
    }

    [Fact]
    public async Task RepeatedReconnect_ReceivesSnapshotEachTime()
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

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var connection = SignalRTestSupport.CreateConnection(host, token);
            DeliveryEnvelope<RoomProjectionDto>? snapshot = null;
            connection.On<DeliveryEnvelope<RoomProjectionDto>>(
                DeliveryHubMethods.ReceiveSnapshot,
                envelope => snapshot = envelope);

            await connection.StartAsync();
            await connection.InvokeAsync("JoinRoom", room.Id.Value);
            await SignalRTestSupport.WaitForAsync(() => snapshot is not null);

            snapshot!.IsSnapshot.Should().BeTrue();
            snapshot.AggregateVersion.Should().Be(room.Version.Value);
            await connection.StopAsync();
        }
    }

    [Fact]
    public async Task DuplicatePublish_SendsMonotonicallyIncreasingVersions()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateLobbyRoom();
        room.Join(ParticipantId.New(), "Guest");
        await SaveRoomAsync(host, room);

        var hostParticipant = room.Participants.Single(p => p.IsHost);
        await using var connection = await ConnectBoundAsync(host, room.Id.Value, hostParticipant.Id.Value);

        var updates = new List<DeliveryEnvelope<RoomProjectionDto>>();
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(
            DeliveryHubMethods.ReceiveSnapshot,
            envelope => updates.Add(envelope));
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(
            DeliveryHubMethods.ReceiveUpdate,
            envelope => updates.Add(envelope));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => updates.Count == 1);

        var guest = room.Participants.First(p => !p.IsHost);
        room.ClearPendingEvents();
        room.AssignTeam(guest.Id, Team.Red);
        await PublishCommittedChangeAsync(host, room);

        room.ClearPendingEvents();
        room.AssignTeam(hostParticipant.Id, Team.Blue);
        await PublishCommittedChangeAsync(host, room);

        await SignalRTestSupport.WaitForAsync(() => updates.Count(u => !u.IsSnapshot) == 2);

        var versions = updates.Select(u => u.AggregateVersion).ToList();
        versions.Should().BeInAscendingOrder();
        versions.Distinct().Should().HaveCount(versions.Count);

        await connection.StopAsync();
    }

    [Fact]
    public async Task RejoinAfterRoleChange_ReflectsCommittedParticipantRole()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateLobbyRoom();
        var guestId = ParticipantId.New();
        room.Join(guestId, "Guest");
        room.AssignTeam(guestId, Team.Red);
        await SaveRoomAsync(host, room);

        room.ClearPendingEvents();
        room.AssignRole(guestId, Role.Spymaster);
        await PublishCommittedChangeAsync(host, room);

        await using var connection = await ConnectBoundAsync(host, room.Id.Value, guestId.Value);
        DeliveryEnvelope<RoomProjectionDto>? snapshot = null;
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(
            DeliveryHubMethods.ReceiveSnapshot,
            envelope => snapshot = envelope);

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => snapshot is not null);

        var guestProjection = snapshot!.Projection.Participants.Single(p => p.ParticipantId == guestId.Value);
        guestProjection.Role.Should().Be(Role.Spymaster.Value);

        await connection.StopAsync();
    }

    [Fact]
    public async Task ProjectionConsistency_SnapshotMatchesBuilderAndFilter()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateRoomWithMatchStarted();
        var operative = room.Participants.First(p => p.Role == Role.Operative);
        await SaveRoomAsync(host, room);

        await using var connection = await ConnectBoundAsync(host, room.Id.Value, operative.Id.Value);
        DeliveryEnvelope<RoomProjectionDto>? snapshot = null;
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(
            DeliveryHubMethods.ReceiveSnapshot,
            envelope => snapshot = envelope);

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => snapshot is not null);

        using var scope = host.Services.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IProjectionBuilder>();
        var filter = scope.ServiceProvider.GetRequiredService<IVisibilityFilter>();
        var expected = filter.Filter(builder.Build(room), operative.Role, operative.Team);

        snapshot!.Projection.Should().BeEquivalentTo(expected);

        await connection.StopAsync();
    }

    [Fact]
    public async Task RecoveryAfterNewHostScope_DeliversCommittedSnapshot()
    {
        await using var host = await SignalRTestHost.CreateAsync(_database.ConnectionString);
        var room = RoomTestData.CreateRoomWithMatchStarted();
        await SaveRoomAsync(host, room);

        var spymaster = room.Participants.First(p => p.Role == Role.Spymaster);
        await using var connection = await ConnectBoundAsync(host, room.Id.Value, spymaster.Id.Value);
        DeliveryEnvelope<RoomProjectionDto>? snapshot = null;
        connection.On<DeliveryEnvelope<RoomProjectionDto>>(
            DeliveryHubMethods.ReceiveSnapshot,
            envelope => snapshot = envelope);

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", room.Id.Value);
        await SignalRTestSupport.WaitForAsync(() => snapshot is not null);

        snapshot!.AggregateVersion.Should().Be(room.Version.Value);
        snapshot.Projection.Board.Should().NotBeNull();

        await connection.StopAsync();
    }

    private static async Task<HubConnection> ConnectBoundAsync(
        SignalRTestHost host,
        Guid roomId,
        Guid participantId)
    {
        var token = await SignalRTestSupport.BindParticipantAndGetAccessTokenAsync(host, roomId, participantId);
        return SignalRTestSupport.CreateConnection(host, token);
    }

    private static async Task PublishCommittedChangeAsync(SignalRTestHost host, Room room)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var custody = scope.ServiceProvider.GetRequiredService<IRoomCustody>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        await custody.SaveAsync(room);
        await publisher.PublishAsync(room.GetPendingEvents());
        room.ClearPendingEvents();
    }

    private static async Task SaveRoomAsync(SignalRTestHost host, Room room)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var custody = scope.ServiceProvider.GetRequiredService<IRoomCustody>();
        await custody.SaveAsync(room);
        room.ClearPendingEvents();
    }
}

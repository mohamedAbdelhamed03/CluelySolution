using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cluely.Api.Contracts.Requests;
using Cluely.Api.Contracts.Responses;
using Cluely.Api.Infrastructure;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cluely.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class RoomsApiTests
{
    private readonly SqlServerTestDatabase _database;

    public RoomsApiTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task CreateRoom_ReturnsCreated_WithRoomIdentifiers()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest
        {
            HostNickname = "HostPlayer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateRoomResponse>();
        body.Should().NotBeNull();
        body!.RoomId.Should().NotBeEmpty();
        body.RoomCode.Should().NotBeNullOrWhiteSpace();
        response.Headers.Contains(CorrelationId.HeaderName).Should().BeTrue();
    }

    [Fact]
    public async Task CreateRoom_WithEmptyNickname_ReturnsValidationProblem()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest
        {
            HostNickname = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (response.Content.Headers.ContentType?.MediaType).Should().BeOneOf("application/problem+json", "application/json");
    }

    [Fact]
    public async Task JoinRoom_WithUnknownCode_ReturnsNotFound()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.PostAsJsonAsync("/api/rooms/UNKNOWN1/join", new JoinRoomRequest
        {
            Nickname = "Guest"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(404);
    }

    [Fact]
    public async Task JoinRoom_AfterCreate_ReturnsParticipantId()
    {
        await using var factory = await CreateFactoryAsync();
        using var hostClient = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(hostClient);

        var createResponse = await hostClient.PostAsJsonAsync("/api/rooms", new CreateRoomRequest
        {
            HostNickname = "HostPlayer"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateRoomResponse>();

        using var guestClient = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(guestClient);

        var joinResponse = await guestClient.PostAsJsonAsync($"/api/rooms/{created!.RoomCode}/join", new JoinRoomRequest
        {
            Nickname = "Guest"
        });

        joinResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var joined = await joinResponse.Content.ReadFromJsonAsync<JoinRoomResponse>();
        joined!.ParticipantId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRoom_WithUnknownId_ReturnsNotFound()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.GetAsync($"/api/rooms/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRoom_AfterCreate_ReturnsSummary()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var created = await CreateRoomAsync(client);
        var response = await client.GetAsync($"/api/rooms/{created.RoomId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<RoomSummaryResponse>();
        summary!.RoomId.Should().Be(created.RoomId);
        summary.RoomCode.Should().Be(created.RoomCode);
        summary.State.Should().Be("Lobby");
    }

    [Fact]
    public async Task MalformedJson_ReturnsBadRequest()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.PostAsync(
            "/api/rooms",
            new StringContent("{ invalid", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<CreateRoomResponse> CreateRoomAsync(HttpClient client)
    {
        if (client.DefaultRequestHeaders.Authorization is null)
        {
            await AuthTestHelper.AuthenticateClientAsync(client);
        }

        var response = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest
        {
            HostNickname = "HostPlayer"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateRoomResponse>())!;
    }

    private async Task<ApiTestFactory> CreateFactoryAsync()
    {
        var factory = new ApiTestFactory(_database.ConnectionString);
        await factory.InitializeDatabaseAsync();
        return factory;
    }
}

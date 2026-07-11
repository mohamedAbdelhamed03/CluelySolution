using System.Net;
using System.Net.Http.Json;
using Cluely.Api.Contracts.Requests;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cluely.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class ParticipantBindingApiTests
{
    private readonly SqlServerTestDatabase _database;

    public ParticipantBindingApiTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Gameplay_WithoutRoomBinding_ReturnsForbidden()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.PostAsync($"/api/rooms/{Guid.NewGuid()}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(403);
        problem.Extensions["code"]!.ToString().Should().Be("ParticipantBindingNotFound");
        problem.Extensions.Should().ContainKey("correlationId");
    }

    [Fact]
    public async Task Projection_WithoutRoomBinding_ReturnsForbidden()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.GetAsync($"/api/rooms/{Guid.NewGuid()}/projection");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["code"]!.ToString().Should().Be("ParticipantBindingNotFound");
    }

    private async Task<ApiTestFactory> CreateFactoryAsync()
    {
        var factory = new ApiTestFactory(_database.ConnectionString);
        await factory.InitializeDatabaseAsync();
        return factory;
    }
}

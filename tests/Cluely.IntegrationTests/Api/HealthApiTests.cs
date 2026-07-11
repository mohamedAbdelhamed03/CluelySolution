using System.Net;
using System.Net.Http.Json;
using Cluely.Api.Contracts.Responses;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Xunit;

namespace Cluely.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class HealthApiTests
{
    private readonly SqlServerTestDatabase _database;

    public HealthApiTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthy()
    {
        await using var factory = new ApiTestFactory(_database.ConnectionString);
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.Should().Be("Healthy");
    }
}

using System.Net;
using System.Text.Json;
using Cluely.Application.Common;
using Cluely.Infrastructure.Middleware;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace Cluely.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class ProductionReadinessApiTests
{
    private readonly SqlServerTestDatabase _database;

    public ProductionReadinessApiTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Responses_IncludeSecurityAndCorrelationHeaders()
    {
        await using var factory = new ApiTestFactory(_database.ConnectionString);
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle("DENY");
        response.Headers.Should().ContainKey(CorrelationIdConstants.HeaderName);
    }

    [Fact]
    public async Task Cors_AllowsConfiguredOrigin_AndRejectsUntrustedOrigin()
    {
        await using var factory = new ApiTestFactory(_database.ConnectionString);
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();
        using var trustedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        trustedRequest.Headers.Add("Origin", "https://frontend.test");
        using var untrustedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        untrustedRequest.Headers.Add("Origin", "https://evil.test");

        var trustedResponse = await client.SendAsync(trustedRequest);
        var untrustedResponse = await client.SendAsync(untrustedRequest);

        trustedResponse.Headers.GetValues("Access-Control-Allow-Origin")
            .Should()
            .ContainSingle("https://frontend.test");
        untrustedResponse.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task OpenApi_DocumentsEveryControllerOperation_AndRequiredPublishKey()
    {
        await using var factory = new ApiTestFactory(_database.ConnectionString);
        using var scope = factory.Services.CreateScope();
        var swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var apiDescriptions = scope.ServiceProvider
            .GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(description => description.HttpMethod is not null)
            .ToList();

        foreach (var description in apiDescriptions)
        {
            var path = "/" + description.RelativePath;
            swagger.Paths.Should().ContainKey(path);
            swagger.Paths[path].Operations.Keys
                .Select(method => method.ToString().ToUpperInvariant())
                .Should()
                .Contain(description.HttpMethod!, $"OpenAPI must include {description.HttpMethod} {path}");
        }

        var publish = swagger.Paths["/api/content/{id}/publish"].Operations.Values.Single();
        var idempotencyKey = publish.Parameters.Single(parameter => parameter.Name == "Idempotency-Key");
        idempotencyKey.Required.Should().BeTrue();
        idempotencyKey.Description.Should().Contain("original published version");
    }

    [Fact]
    public async Task UnexpectedException_ReturnsSanitizedProblemDetails()
    {
        const string sensitiveMessage = "database password leaked";
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException(sensitiveMessage),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items[CorrelationIdConstants.ItemKey] = Guid.NewGuid().ToString();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        document.RootElement.GetProperty("detail").GetString().Should().Be("An unexpected server error occurred.");
        document.RootElement.ToString().Should().NotContain(sensitiveMessage);
    }
}

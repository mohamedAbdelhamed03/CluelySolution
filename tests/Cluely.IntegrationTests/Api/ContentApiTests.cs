using System.Net;
using System.Net.Http.Json;
using Cluely.Api.Contracts.Requests;
using Cluely.Api.Contracts.Responses;
using Cluely.Api.Infrastructure;
using Cluely.Domain.Content;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cluely.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class ContentApiTests
{
    private readonly SqlServerTestDatabase _database;

    public ContentApiTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/content", ValidCreateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithDictionaryId()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.PostAsJsonAsync("/api/content", ValidCreateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ContentCreatedResponse>();
        body.Should().NotBeNull();
        body!.DictionaryId.Should().NotBeEmpty();
        body.Title.Should().Be("Party Pack");
        response.Headers.Contains(CorrelationId.HeaderName).Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsValidationProblem()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.PostAsJsonAsync("/api/content", new CreateContentRequest
        {
            Title = "",
            Description = "Fun words",
            Language = "en",
            ContentType = "user"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (response.Content.Headers.ContentType?.MediaType).Should().BeOneOf("application/problem+json", "application/json");
    }

    [Fact]
    public async Task Mine_AfterCreate_ReturnsOwnedDictionary()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var created = await CreateDictionaryAsync(client);

        var response = await client.GetAsync("/api/content/mine");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaries = await response.Content.ReadFromJsonAsync<List<ContentSummaryResponse>>();
        summaries.Should().ContainSingle(summary => summary.DictionaryId == created.DictionaryId);
    }

    [Fact]
    public async Task Details_WithUnknownId_ReturnsNotFound()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.GetAsync($"/api/content/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(404);
        (response.Content.Headers.ContentType?.MediaType).Should().BeOneOf("application/problem+json", "application/json");
    }

    [Fact]
    public async Task Details_AfterCreate_ReturnsDictionary()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var created = await CreateDictionaryAsync(client);

        var response = await client.GetAsync($"/api/content/{created.DictionaryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<ContentDetailsResponse>();
        details!.DictionaryId.Should().Be(created.DictionaryId);
        details.Title.Should().Be("Party Pack");
    }

    [Fact]
    public async Task Update_OtherUsersDictionary_ReturnsForbidden()
    {
        await using var factory = await CreateFactoryAsync();
        using var ownerClient = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(ownerClient);
        var created = await CreateDictionaryAsync(ownerClient);

        using var otherClient = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(otherClient);

        var response = await otherClient.PatchAsJsonAsync(
            $"/api/content/{created.DictionaryId}",
            new UpdateContentRequest
            {
                Title = "Hijacked",
                Description = "Updated",
                Tags = ["party"],
                Language = "en",
                Region = "US"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["code"]!.ToString().Should().Be("NotOwnerException");
    }

    [Fact]
    public async Task AddWords_Validate_And_Publish_ReturnsVersion()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);
        var created = await CreateDictionaryAsync(client);

        var addResponse = await client.PostAsJsonAsync(
            $"/api/content/{created.DictionaryId}/words",
            new AddWordsRequest { Words = ValidWordBatch(DictionaryValidation.MinWords).ToList() });
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var validateResponse = await client.PostAsync(
            $"/api/content/{created.DictionaryId}/validate",
            null);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validation = await validateResponse.Content.ReadFromJsonAsync<ValidateContentResponse>();
        validation!.IsValid.Should().BeTrue();

        var publishIdempotencyKey = Guid.NewGuid();
        var publishResponse = await PostWithIdempotencyAsync(
            client,
            $"/api/content/{created.DictionaryId}/publish",
            publishIdempotencyKey);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await publishResponse.Content.ReadFromJsonAsync<PublishContentResponse>();
        published!.VersionId.Should().Be(publishIdempotencyKey);
        published.WordCount.Should().Be(DictionaryValidation.MinWords);
        published.VersionLabel.Should().Be(1);

        var versionsResponse = await client.GetAsync($"/api/content/{created.DictionaryId}/versions");
        versionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await versionsResponse.Content.ReadFromJsonAsync<List<ContentVersionResponse>>();
        versions.Should().ContainSingle(version => version.VersionId == published.VersionId);
    }

    [Fact]
    public async Task Archive_And_Restore_Succeed()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);
        var created = await CreateDictionaryAsync(client);

        var archiveResponse = await client.DeleteAsync($"/api/content/{created.DictionaryId}");
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var restoreResponse = await client.PostAsync($"/api/content/{created.DictionaryId}/restore", null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Approve_WithoutModerator_ReturnsForbidden()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);
        var created = await CreateDictionaryAsync(client);

        await client.PostAsJsonAsync(
            $"/api/content/{created.DictionaryId}/words",
            new AddWordsRequest { Words = ValidWordBatch(DictionaryValidation.MinWords).ToList() });
        await client.PostAsync($"/api/content/{created.DictionaryId}/validate", null);
        var publishResponse = await PostWithIdempotencyAsync(
            client,
            $"/api/content/{created.DictionaryId}/publish",
            Guid.NewGuid());
        publishResponse.EnsureSuccessStatusCode();
        var published = await publishResponse.Content.ReadFromJsonAsync<PublishContentResponse>();

        var approveResponse = await client.PostAsJsonAsync(
            $"/api/content/{created.DictionaryId}/approve",
            new VersionActionRequest { VersionId = published!.VersionId });

        approveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await approveResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["code"]!.ToString().Should().Be("Forbidden");
    }

    [Fact]
    public async Task Publish_WithDuplicateIdempotencyKey_ReturnsSameVersion()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);
        var created = await CreateDictionaryAsync(client);

        await client.PostAsJsonAsync(
            $"/api/content/{created.DictionaryId}/words",
            new AddWordsRequest { Words = ValidWordBatch(DictionaryValidation.MinWords).ToList() });
        await client.PostAsync($"/api/content/{created.DictionaryId}/validate", null);

        var idempotencyKey = Guid.NewGuid();
        var first = await PostWithIdempotencyAsync(
            client,
            $"/api/content/{created.DictionaryId}/publish",
            idempotencyKey);
        var second = await PostWithIdempotencyAsync(
            client,
            $"/api/content/{created.DictionaryId}/publish",
            idempotencyKey);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<PublishContentResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<PublishContentResponse>();
        secondBody.Should().BeEquivalentTo(firstBody);
        firstBody!.VersionId.Should().Be(idempotencyKey);

        var versionsResponse = await client.GetAsync($"/api/content/{created.DictionaryId}/versions");
        var versions = await versionsResponse.Content.ReadFromJsonAsync<List<ContentVersionResponse>>();
        versions.Should().ContainSingle();
    }

    [Fact]
    public async Task Discover_ReturnsOk_WhenAuthenticated()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.GetAsync("/api/content/discover");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static CreateContentRequest ValidCreateRequest() =>
        new()
        {
            Title = "Party Pack",
            Description = "Fun words for parties",
            Tags = ["party"],
            Language = "en",
            Region = "US",
            ContentType = "user"
        };

    private static UpdateContentRequest ValidUpdateRequest() =>
        new()
        {
            Title = "Renamed Pack",
            Description = "Updated",
            Tags = ["party"],
            Language = "en",
            Region = "US"
        };

    private static IEnumerable<string> ValidWordBatch(int count)
    {
        for (var index = 0; index < count; index++)
        {
            yield return $"word{index + 1}";
        }
    }

    private static async Task<HttpResponseMessage> PostWithIdempotencyAsync(
        HttpClient client,
        string url,
        Guid idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add(IdempotencyKeyAccessor.HeaderName, idempotencyKey.ToString());
        return await client.SendAsync(request);
    }

    private static async Task<ContentCreatedResponse> CreateDictionaryAsync(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/content")
        {
            Content = JsonContent.Create(ValidCreateRequest())
        };
        request.Headers.Add(IdempotencyKeyAccessor.HeaderName, Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContentCreatedResponse>())!;
    }

    private async Task<ApiTestFactory> CreateFactoryAsync()
    {
        var factory = new ApiTestFactory(_database.ConnectionString);
        await factory.InitializeDatabaseAsync();
        return factory;
    }
}

using System.Net;
using System.Net.Http.Json;
using Cluely.Api.Contracts.Requests;
using Cluely.Api.Contracts.Responses;
using Cluely.Api.Infrastructure;
using Cluely.Domain.Content;
using Cluely.Domain.Content.ValueObjects;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cluely.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class ContentWorkflowE2ETests
{
    private readonly SqlServerTestDatabase _database;

    public ContentWorkflowE2ETests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task FullContentLifecycle_FromRegisterThroughRetire_Succeeds()
    {
        await using var factory = await CreateFactoryAsync();

        using var ownerClient = factory.CreateClient();
        var ownerLogin = await AuthTestHelper.RegisterAndLoginAsync(ownerClient);
        AuthTestHelper.SetBearerToken(ownerClient, ownerLogin.AccessToken);

        var createResponse = await SendWithIdempotencyAsync(
            ownerClient,
            HttpMethod.Post,
            "/api/content",
            JsonContent.Create(ValidCreateRequest()),
            Guid.NewGuid());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ContentCreatedResponse>();

        await ownerClient.PostAsJsonAsync(
            $"/api/content/{created!.DictionaryId}/words",
            new AddWordsRequest { Words = ValidWordBatch(DictionaryValidation.MinWords).ToList() });

        var validateResponse = await ownerClient.PostAsync(
            $"/api/content/{created.DictionaryId}/validate",
            null);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publishKey = Guid.NewGuid();
        var publishResponse = await SendWithIdempotencyAsync(
            ownerClient,
            HttpMethod.Post,
            $"/api/content/{created.DictionaryId}/publish",
            content: null,
            publishKey);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await publishResponse.Content.ReadFromJsonAsync<PublishContentResponse>();
        published!.VersionId.Should().Be(publishKey);

        var retryPublish = await SendWithIdempotencyAsync(
            ownerClient,
            HttpMethod.Post,
            $"/api/content/{created.DictionaryId}/publish",
            content: null,
            publishKey);
        retryPublish.StatusCode.Should().Be(HttpStatusCode.OK);
        (await retryPublish.Content.ReadFromJsonAsync<PublishContentResponse>())!
            .Should()
            .BeEquivalentTo(published);

        (await ownerClient.GetAsync("/api/content/discover")).StatusCode.Should().Be(HttpStatusCode.OK);

        var mineResponse = await ownerClient.GetAsync("/api/content/mine");
        mineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var mine = await mineResponse.Content.ReadFromJsonAsync<List<ContentSummaryResponse>>();
        mine.Should().ContainSingle(summary => summary.DictionaryId == created.DictionaryId);

        var cloneKey = Guid.NewGuid();
        var cloneResponse = await SendWithIdempotencyAsync(
            ownerClient,
            HttpMethod.Post,
            $"/api/content/{created.DictionaryId}/clone",
            JsonContent.Create(new CloneContentRequest { SourceVersionId = published.VersionId }),
            cloneKey);
        cloneResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var clone = await cloneResponse.Content.ReadFromJsonAsync<CloneContentResponse>();
        clone!.SourceDictionaryId.Should().Be(created.DictionaryId);
        clone.SourceVersionId.Should().Be(published.VersionId);

        await SetDictionaryVisibilityAsync(factory, created.DictionaryId, ownerLogin.UserId, Visibility.Shared);

        using var granteeClient = factory.CreateClient();
        var granteeLogin = await AuthTestHelper.RegisterAndLoginAsync(granteeClient);
        AuthTestHelper.SetBearerToken(granteeClient, granteeLogin.AccessToken);

        var shareResponse = await ownerClient.PostAsJsonAsync(
            $"/api/content/{created.DictionaryId}/share",
            new ShareContentRequest { GranteeId = granteeLogin.UserId });
        shareResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var granteeDetails = await granteeClient.GetAsync($"/api/content/{created.DictionaryId}");
        granteeDetails.StatusCode.Should().Be(HttpStatusCode.OK);

        var ownerDetails = await ownerClient.GetAsync($"/api/content/{created.DictionaryId}");
        ownerDetails.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await ownerDetails.Content.ReadFromJsonAsync<ContentDetailsResponse>();
        details!.DictionaryId.Should().Be(created.DictionaryId);
        details.Versions.Should().ContainSingle(version => version.VersionId == published.VersionId);

        ownerClient.DefaultRequestHeaders.Add(TestContentModeratorAccessor.HeaderName, "true");
        var retireResponse = await ownerClient.PostAsJsonAsync(
            $"/api/content/{created.DictionaryId}/retire",
            new VersionActionRequest { VersionId = published.VersionId });
        retireResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var versionsAfterRetire = await ownerClient.GetAsync($"/api/content/{created.DictionaryId}/versions");
        versionsAfterRetire.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await versionsAfterRetire.Content.ReadFromJsonAsync<List<ContentVersionResponse>>();
        versions.Should().ContainSingle(version =>
            version.VersionId == published.VersionId && version.LifecycleState == "Retired");
    }

    private static CreateContentRequest ValidCreateRequest() =>
        new()
        {
            Title = "Workflow Pack",
            Description = "End-to-end workflow dictionary",
            Tags = ["workflow"],
            Language = "en",
            Region = "US",
            ContentType = "user"
        };

    private static IEnumerable<string> ValidWordBatch(int count)
    {
        for (var index = 0; index < count; index++)
        {
            yield return $"word{index + 1}";
        }
    }

    private static async Task SetDictionaryVisibilityAsync(
        ApiTestFactory factory,
        Guid dictionaryId,
        Guid ownerUserId,
        Visibility visibility)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<Cluely.Application.Common.Ports.Content.IDictionaryRepository>();
        var dictionary = await repository.GetAsync(DictionaryId.From(dictionaryId));
        dictionary!.SetVisibility(OwnerId.From(ownerUserId), visibility);
        await repository.UpdateAsync(dictionary);
    }

    private static Task<HttpResponseMessage> SendWithIdempotencyAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        HttpContent? content,
        Guid idempotencyKey)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Add(IdempotencyKeyAccessor.HeaderName, idempotencyKey.ToString());
        return client.SendAsync(request);
    }

    private async Task<ApiTestFactory> CreateFactoryAsync()
    {
        var factory = new ApiTestFactory(_database.ConnectionString);
        await factory.InitializeDatabaseAsync();
        return factory;
    }
}

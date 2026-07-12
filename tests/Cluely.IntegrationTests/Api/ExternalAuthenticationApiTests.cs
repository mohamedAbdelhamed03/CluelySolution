using System.Net;
using System.Net.Http.Json;
using Cluely.Api.Contracts.Requests;
using Cluely.Api.Contracts.Responses;
using Cluely.Application.Common.Ports.Identity;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Xunit;

namespace Cluely.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class ExternalAuthenticationApiTests
{
    private readonly SqlServerTestDatabase _database;

    public ExternalAuthenticationApiTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Theory]
    [InlineData(ExternalAuthProviders.Google)]
    [InlineData(ExternalAuthProviders.Facebook)]
    [InlineData(ExternalAuthProviders.Apple)]
    public async Task ExternalLogin_FirstLogin_CreatesUserAndIssuesTokens(string provider)
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var providerUserId = $"{provider}-{Guid.NewGuid():N}";
        var token = $"token-{providerUserId}";
        ConfigureValidToken(factory, provider, token, providerUserId, $"{providerUserId}@external.test", emailVerified: true);

        var response = await client.PostAsJsonAsync("/api/auth/external", new ExternalLoginRequest
        {
            Provider = provider,
            Token = token
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginUserResponse>();
        body!.UserId.Should().NotBeEmpty();
        body.Email.Should().Be($"{providerUserId}@external.test");
        body.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExternalLogin_ExistingUser_ReturnsTokens()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var providerUserId = Guid.NewGuid().ToString("N");
        var token = $"existing-{providerUserId}";
        ConfigureValidToken(factory, ExternalAuthProviders.Google, token, providerUserId, "existing@external.test", true);

        var first = await client.PostAsJsonAsync("/api/auth/external", new ExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Google,
            Token = token
        });
        first.EnsureSuccessStatusCode();
        var firstBody = await first.Content.ReadFromJsonAsync<LoginUserResponse>();

        var second = await client.PostAsJsonAsync("/api/auth/external", new ExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Google,
            Token = token
        });
        second.EnsureSuccessStatusCode();
        var secondBody = await second.Content.ReadFromJsonAsync<LoginUserResponse>();

        secondBody!.UserId.Should().Be(firstBody!.UserId);
        secondBody.AccessToken.Should().NotBeNullOrWhiteSpace();
        secondBody.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExternalLogin_RefreshFlow_WorksAfterExternalLogin()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var providerUserId = Guid.NewGuid().ToString("N");
        var token = $"refresh-{providerUserId}";
        ConfigureValidToken(factory, ExternalAuthProviders.Google, token, providerUserId, "refresh@external.test", true);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/external", new ExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Google,
            Token = token
        });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginUserResponse>();

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = login!.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LinkExternalLogin_LinksProviderToAuthenticatedUser()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var login = await AuthTestHelper.RegisterAndLoginAsync(client);
        AuthTestHelper.SetBearerToken(client, login.AccessToken);

        var providerUserId = Guid.NewGuid().ToString("N");
        var token = $"link-{providerUserId}";
        ConfigureValidToken(factory, ExternalAuthProviders.Facebook, token, providerUserId, "facebook@external.test", true);

        var response = await client.PostAsJsonAsync("/api/auth/external/link", new LinkExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Facebook,
            Token = token
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LinkExternalLoginResponse>();
        body!.Provider.Should().Be(ExternalAuthProviders.Facebook);
    }

    [Fact]
    public async Task LinkExternalLogin_DuplicateProvider_ReturnsConflict()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var login = await AuthTestHelper.RegisterAndLoginAsync(client);
        AuthTestHelper.SetBearerToken(client, login.AccessToken);

        var providerUserId = Guid.NewGuid().ToString("N");
        var token = $"dup-link-{providerUserId}";
        ConfigureValidToken(factory, ExternalAuthProviders.Google, token, providerUserId, "dup@external.test", true);

        await client.PostAsJsonAsync("/api/auth/external/link", new LinkExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Google,
            Token = token
        });

        var response = await client.PostAsJsonAsync("/api/auth/external/link", new LinkExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Google,
            Token = token
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task LinkExternalLogin_ProviderAccountAlreadyLinked_ReturnsConflict()
    {
        await using var factory = await CreateFactoryAsync();
        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();

        var providerUserId = Guid.NewGuid().ToString("N");
        var token = $"shared-{providerUserId}";
        ConfigureValidToken(factory, ExternalAuthProviders.Apple, token, providerUserId, "apple@external.test", true);

        var firstLogin = await AuthTestHelper.RegisterAndLoginAsync(firstClient);
        AuthTestHelper.SetBearerToken(firstClient, firstLogin.AccessToken);
        await firstClient.PostAsJsonAsync("/api/auth/external/link", new LinkExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Apple,
            Token = token
        });

        var secondLogin = await AuthTestHelper.RegisterAndLoginAsync(secondClient);
        AuthTestHelper.SetBearerToken(secondClient, secondLogin.AccessToken);
        var response = await secondClient.PostAsJsonAsync("/api/auth/external/link", new LinkExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Apple,
            Token = token
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UnlinkExternalLogin_RemovesLinkedProvider()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var login = await AuthTestHelper.RegisterAndLoginAsync(client);
        AuthTestHelper.SetBearerToken(client, login.AccessToken);

        var providerUserId = Guid.NewGuid().ToString("N");
        var token = $"unlink-{providerUserId}";
        ConfigureValidToken(factory, ExternalAuthProviders.Google, token, providerUserId, "unlink@external.test", true);
        await client.PostAsJsonAsync("/api/auth/external/link", new LinkExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Google,
            Token = token
        });

        var response = await client.DeleteAsync("/api/auth/external/google");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnlinkExternalLogin_LastLoginMethod_ReturnsConflict()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var providerUserId = Guid.NewGuid().ToString("N");
        var token = $"only-{providerUserId}";
        ConfigureValidToken(factory, ExternalAuthProviders.Google, token, providerUserId, "only@external.test", true);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/external", new ExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Google,
            Token = token
        });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginUserResponse>();
        AuthTestHelper.SetBearerToken(client, login!.AccessToken);

        var response = await client.DeleteAsync("/api/auth/external/google");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(ExternalTokenValidationFailureReason.InvalidToken, HttpStatusCode.Unauthorized)]
    [InlineData(ExternalTokenValidationFailureReason.ExpiredToken, HttpStatusCode.Unauthorized)]
    [InlineData(ExternalTokenValidationFailureReason.WrongAudience, HttpStatusCode.Unauthorized)]
    [InlineData(ExternalTokenValidationFailureReason.WrongIssuer, HttpStatusCode.Unauthorized)]
    public async Task ExternalLogin_InvalidTokenScenarios_ReturnExpectedStatus(
        ExternalTokenValidationFailureReason failureReason,
        HttpStatusCode expectedStatus)
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var token = $"invalid-{failureReason}";
        factory.ExternalProviders.Google.TokenResults[token] = new ExternalTokenValidationResult(
            false,
            null,
            failureReason);

        var response = await client.PostAsJsonAsync("/api/auth/external", new ExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Google,
            Token = token
        });

        response.StatusCode.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task ExternalLogin_ProviderUnavailable_ReturnsServiceUnavailable()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var token = "provider-down";
        factory.ExternalProviders.Google.TokenResults[token] = new ExternalTokenValidationResult(
            false,
            null,
            ExternalTokenValidationFailureReason.ProviderUnavailable);

        var response = await client.PostAsJsonAsync("/api/auth/external", new ExternalLoginRequest
        {
            Provider = ExternalAuthProviders.Google,
            Token = token
        });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private static void ConfigureValidToken(
        ApiTestFactory factory,
        string provider,
        string token,
        string providerUserId,
        string email,
        bool emailVerified)
    {
        var providerInstance = provider switch
        {
            ExternalAuthProviders.Google => factory.ExternalProviders.Google,
            ExternalAuthProviders.Facebook => factory.ExternalProviders.Facebook,
            ExternalAuthProviders.Apple => factory.ExternalProviders.Apple,
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };

        providerInstance.TokenResults[token] = new ExternalTokenValidationResult(
            true,
            new ExternalUserInfo(providerUserId, email, emailVerified),
            null);
    }

    private async Task<ApiTestFactory> CreateFactoryAsync()
    {
        var factory = new ApiTestFactory(_database.ConnectionString);
        await factory.InitializeDatabaseAsync();
        return factory;
    }
}

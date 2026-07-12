using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Cluely.Api.Contracts.Requests;
using Cluely.Api.Contracts.Responses;
using Cluely.IntegrationTests.Infrastructure;
using Cluely.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Cluely.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class AuthApiTests
{
    private readonly SqlServerTestDatabase _database;

    public AuthApiTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Register_ReturnsCreated_WithUserIdentity()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var email = $"{Guid.NewGuid():N}@auth.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest
        {
            Email = email,
            Password = TestAuthConfiguration.DefaultPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        body!.UserId.Should().NotBeEmpty();
        body.Email.Should().Be(email.ToLowerInvariant());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@auth.test";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest
        {
            Email = email,
            Password = TestAuthConfiguration.DefaultPassword
        });

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest
        {
            Email = email,
            Password = TestAuthConfiguration.DefaultPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_Success_ReturnsTokens()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@auth.test";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest
        {
            Email = email,
            Password = TestAuthConfiguration.DefaultPassword
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginUserRequest
        {
            Email = email,
            Password = TestAuthConfiguration.DefaultPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginUserResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.AccessTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
        body.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginUserRequest
        {
            Email = "missing@auth.test",
            Password = TestAuthConfiguration.DefaultPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndInvalidatesPrevious()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var login = await AuthTestHelper.RegisterAndLoginAsync(client);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = login.RefreshToken
        });
        refreshResponse.EnsureSuccessStatusCode();
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>();

        var oldRefreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = login.RefreshToken
        });
        oldRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        refreshed!.RefreshToken.Should().NotBe(login.RefreshToken);
        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_ConcurrentReplay_AllowsExactlyOneRotation()
    {
        await using var factory = await CreateFactoryAsync();
        using var registrationClient = factory.CreateClient();
        var login = await AuthTestHelper.RegisterAndLoginAsync(registrationClient);
        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();

        var firstRequest = firstClient.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = login.RefreshToken
        });
        var secondRequest = secondClient.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = login.RefreshToken
        });

        var responses = await Task.WhenAll(firstRequest, secondRequest);

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized).Should().Be(1);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var login = await AuthTestHelper.RegisterAndLoginAsync(client);

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new LogoutUserRequest
        {
            RefreshToken = login.RefreshToken
        });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = login.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUser_WhenAuthenticated()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var login = await AuthTestHelper.RegisterAndLoginAsync(client);
        AuthTestHelper.SetBearerToken(client, login.AccessToken);

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        body!.UserId.Should().Be(login.UserId);
        body.Email.Should().Be(login.Email);
        body.AccountStatus.Should().Be("Active");
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRoom_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest
        {
            HostNickname = "Host"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRoom_WithValidToken_ReturnsCreated()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthTestHelper.AuthenticateClientAsync(client);

        var response = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest
        {
            HostNickname = "Host"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task MalformedJwt_ReturnsUnauthorized()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        AuthTestHelper.SetBearerToken(client, "not.a.jwt");

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidSignatureJwt_ReturnsUnauthorized()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var token = CreateJwt(
            TestAuthConfiguration.Issuer,
            TestAuthConfiguration.Audience,
            "wrong-signing-key-minimum-32-characters!",
            DateTime.UtcNow.AddMinutes(15));

        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredJwt_ReturnsUnauthorized()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var token = CreateJwt(
            TestAuthConfiguration.Issuer,
            TestAuthConfiguration.Audience,
            TestAuthConfiguration.SigningKey,
            DateTime.UtcNow.AddMinutes(-10));

        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignalR_AnonymousConnection_IsRejected()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var server = factory.Server;

        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri("http://localhost/hubs/game"), options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();

        var act = async () => await connection.StartAsync();
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task SignalR_AuthenticatedConnection_CanJoinRoom()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var login = await AuthTestHelper.RegisterAndLoginAsync(client);
        AuthTestHelper.SetBearerToken(client, login.AccessToken);

        var createResponse = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest
        {
            HostNickname = "Host"
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateRoomResponse>();

        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri("http://localhost/hubs/game"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(login.AccessToken);
            })
            .Build();

        await connection.StartAsync();
        await connection.InvokeAsync("JoinRoom", created!.RoomId);
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    private static string CreateJwt(string issuer, string audience, string signingKey, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim("userId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "jwt@test.local")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<ApiTestFactory> CreateFactoryAsync()
    {
        var factory = new ApiTestFactory(_database.ConnectionString);
        await factory.InitializeDatabaseAsync();
        return factory;
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cluely.Api.Contracts.Requests;
using Cluely.Api.Contracts.Responses;

namespace Cluely.IntegrationTests.Infrastructure;

public static class AuthTestHelper
{
    public static async Task<LoginUserResponse> RegisterAndLoginAsync(
        HttpClient client,
        string? email = null,
        string? password = null)
    {
        email ??= $"{Guid.NewGuid():N}@test.local";
        password ??= TestAuthConfiguration.DefaultPassword;

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest
        {
            Email = email,
            Password = password
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginUserRequest
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        return (await loginResponse.Content.ReadFromJsonAsync<LoginUserResponse>())!;
    }

    public static void SetBearerToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static async Task AuthenticateClientAsync(HttpClient client, string? email = null)
    {
        var login = await RegisterAndLoginAsync(client, email);
        SetBearerToken(client, login.AccessToken);
    }
}

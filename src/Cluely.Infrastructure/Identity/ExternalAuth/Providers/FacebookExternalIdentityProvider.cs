using System.Net;
using System.Text.Json;
using Cluely.Application.Common.Ports.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cluely.Infrastructure.Identity.ExternalAuth.Providers;

public sealed class FacebookExternalIdentityProvider : IExternalIdentityProvider
{
    private readonly HttpClient _httpClient;
    private readonly FacebookAuthOptions _options;
    private readonly ILogger<FacebookExternalIdentityProvider> _logger;

    public FacebookExternalIdentityProvider(
        HttpClient httpClient,
        IOptions<ExternalAuthOptions> options,
        ILogger<FacebookExternalIdentityProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Facebook;
        _logger = logger;
    }

    public string ProviderName => ExternalAuthProviders.Facebook;

    public async Task<ExternalTokenValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            _logger.LogWarning("Facebook external authentication is not configured.");
            return Invalid(ExternalTokenValidationFailureReason.ProviderUnavailable);
        }

        var debugResult = await DebugTokenAsync(token, cancellationToken);
        if (debugResult is not null)
        {
            return debugResult;
        }

        var userInfo = await GetUserInfoAsync(token, cancellationToken);
        if (userInfo is null)
        {
            return Invalid(ExternalTokenValidationFailureReason.InvalidToken);
        }

        return Valid(userInfo);
    }

    private async Task<ExternalTokenValidationResult?> DebugTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var appAccessToken = $"{_options.AppId}|{_options.AppSecret}";
        var requestUri =
            $"https://graph.facebook.com/debug_token?input_token={Uri.EscapeDataString(token)}&access_token={Uri.EscapeDataString(appAccessToken)}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Facebook debug_token request failed.");
            return Invalid(ExternalTokenValidationFailureReason.ProviderUnavailable);
        }

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            return Invalid(ExternalTokenValidationFailureReason.InvalidToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            return Invalid(ExternalTokenValidationFailureReason.ProviderUnavailable);
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("data", out var dataElement))
        {
            return Invalid(ExternalTokenValidationFailureReason.InvalidToken);
        }

        if (!dataElement.TryGetProperty("is_valid", out var validElement)
            || validElement.ValueKind != JsonValueKind.True)
        {
            if (dataElement.TryGetProperty("error", out var errorElement)
                && errorElement.TryGetProperty("code", out var codeElement)
                && codeElement.TryGetInt32(out var code)
                && code is 190 or 463 or 467)
            {
                return Invalid(ExternalTokenValidationFailureReason.ExpiredToken);
            }

            return Invalid(ExternalTokenValidationFailureReason.InvalidToken);
        }

        if (!dataElement.TryGetProperty("app_id", out var appIdElement)
            || !string.Equals(appIdElement.GetString(), _options.AppId, StringComparison.Ordinal))
        {
            return Invalid(ExternalTokenValidationFailureReason.WrongAudience);
        }

        return null;
    }

    private async Task<ExternalUserInfo?> GetUserInfoAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://graph.facebook.com/me?fields=id,email&access_token={Uri.EscapeDataString(token)}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Facebook user info request failed.");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (!root.TryGetProperty("id", out var idElement)
            || string.IsNullOrWhiteSpace(idElement.GetString()))
        {
            return null;
        }

        var email = root.TryGetProperty("email", out var emailElement)
            ? emailElement.GetString()
            : null;

        return new ExternalUserInfo(
            idElement.GetString()!,
            email,
            EmailVerified: !string.IsNullOrWhiteSpace(email));
    }

    private static ExternalTokenValidationResult Valid(ExternalUserInfo userInfo)
        => new(true, userInfo, null);

    private static ExternalTokenValidationResult Invalid(ExternalTokenValidationFailureReason reason)
        => new(false, null, reason);
}

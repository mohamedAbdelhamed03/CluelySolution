using System.Net;
using System.Text.Json;
using Cluely.Application.Common.Ports.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cluely.Infrastructure.Identity.ExternalAuth.Providers;

public sealed class GoogleExternalIdentityProvider : IExternalIdentityProvider
{
    private static readonly string[] ValidIssuers =
    [
        "accounts.google.com",
        "https://accounts.google.com"
    ];

    private readonly HttpClient _httpClient;
    private readonly GoogleAuthOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GoogleExternalIdentityProvider> _logger;

    public GoogleExternalIdentityProvider(
        HttpClient httpClient,
        IOptions<ExternalAuthOptions> options,
        TimeProvider timeProvider,
        ILogger<GoogleExternalIdentityProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Google;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string ProviderName => ExternalAuthProviders.Google;

    public async Task<ExternalTokenValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            _logger.LogWarning("Google external authentication is not configured.");
            return Invalid(ExternalTokenValidationFailureReason.ProviderUnavailable);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(
                $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(token)}",
                cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Google token validation request failed.");
            return Invalid(ExternalTokenValidationFailureReason.ProviderUnavailable);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest
            || response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Invalid(ExternalTokenValidationFailureReason.InvalidToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            return Invalid(ExternalTokenValidationFailureReason.ProviderUnavailable);
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (root.TryGetProperty("error", out _))
        {
            return Invalid(ExternalTokenValidationFailureReason.InvalidToken);
        }

        if (!root.TryGetProperty("sub", out var subjectElement)
            || string.IsNullOrWhiteSpace(subjectElement.GetString()))
        {
            return Invalid(ExternalTokenValidationFailureReason.InvalidToken);
        }

        if (!root.TryGetProperty("aud", out var audienceElement)
            || !string.Equals(audienceElement.GetString(), _options.ClientId, StringComparison.Ordinal))
        {
            return Invalid(ExternalTokenValidationFailureReason.WrongAudience);
        }

        if (!root.TryGetProperty("iss", out var issuerElement)
            || !ValidIssuers.Contains(issuerElement.GetString(), StringComparer.Ordinal))
        {
            return Invalid(ExternalTokenValidationFailureReason.WrongIssuer);
        }

        if (!TryReadExpiry(root, out var expiresAt) || expiresAt <= _timeProvider.GetUtcNow().UtcDateTime)
        {
            return Invalid(ExternalTokenValidationFailureReason.ExpiredToken);
        }

        var email = root.TryGetProperty("email", out var emailElement)
            ? emailElement.GetString()
            : null;
        var emailVerified = root.TryGetProperty("email_verified", out var verifiedElement)
            && (verifiedElement.ValueKind == JsonValueKind.True
                || (verifiedElement.ValueKind == JsonValueKind.String
                    && bool.TryParse(verifiedElement.GetString(), out var parsed)
                    && parsed));

        return Valid(new ExternalUserInfo(subjectElement.GetString()!, email, emailVerified));
    }

    private static bool TryReadExpiry(JsonElement root, out DateTime expiresAt)
    {
        expiresAt = default;
        if (!root.TryGetProperty("exp", out var expiryElement))
        {
            return false;
        }

        if (expiryElement.ValueKind == JsonValueKind.Number && expiryElement.TryGetInt64(out var unixSeconds))
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            return true;
        }

        return false;
    }

    private static ExternalTokenValidationResult Valid(ExternalUserInfo userInfo)
        => new(true, userInfo, null);

    private static ExternalTokenValidationResult Invalid(ExternalTokenValidationFailureReason reason)
        => new(false, null, reason);
}

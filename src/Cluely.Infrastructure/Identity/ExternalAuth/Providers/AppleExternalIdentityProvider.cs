using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cluely.Application.Common.Ports.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Cluely.Infrastructure.Identity.ExternalAuth.Providers;

public sealed class AppleExternalIdentityProvider : IExternalIdentityProvider
{
    private const string AppleIssuer = "https://appleid.apple.com";
    private static readonly string MetadataAddress = $"{AppleIssuer}/.well-known/openid-configuration";

    private readonly AppleAuthOptions _options;
    private readonly ILogger<AppleExternalIdentityProvider> _logger;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public AppleExternalIdentityProvider(
        IOptions<ExternalAuthOptions> options,
        ILogger<AppleExternalIdentityProvider> logger)
    {
        _options = options.Value.Apple;
        _logger = logger;
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            MetadataAddress,
            new OpenIdConnectConfigurationRetriever());
    }

    public string ProviderName => ExternalAuthProviders.Apple;

    public async Task<ExternalTokenValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            _logger.LogWarning("Apple external authentication is not configured.");
            return Invalid(ExternalTokenValidationFailureReason.ProviderUnavailable);
        }

        OpenIdConnectConfiguration configuration;
        try
        {
            configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Apple OpenID configuration retrieval failed.");
            return Invalid(ExternalTokenValidationFailureReason.ProviderUnavailable);
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AppleIssuer,
            ValidateAudience = true,
            ValidAudience = _options.ClientId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
        };

        try
        {
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(subject))
            {
                return Invalid(ExternalTokenValidationFailureReason.InvalidToken);
            }

            var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
            var emailVerifiedClaim = principal.FindFirstValue("email_verified");
            var emailVerified = string.Equals(emailVerifiedClaim, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(emailVerifiedClaim, "1", StringComparison.OrdinalIgnoreCase);

            return Valid(new ExternalUserInfo(subject, email, emailVerified));
        }
        catch (SecurityTokenExpiredException)
        {
            return Invalid(ExternalTokenValidationFailureReason.ExpiredToken);
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return Invalid(ExternalTokenValidationFailureReason.WrongAudience);
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return Invalid(ExternalTokenValidationFailureReason.WrongIssuer);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Apple identity token validation failed.");
            return Invalid(ExternalTokenValidationFailureReason.InvalidToken);
        }
    }

    private ExternalTokenValidationResult Valid(ExternalUserInfo userInfo)
        => new(true, userInfo, null);

    private static ExternalTokenValidationResult Invalid(ExternalTokenValidationFailureReason reason)
        => new(false, null, reason);
}

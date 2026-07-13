namespace Cluely.Application.Common.Ports.Identity;

public static class ExternalAuthProviders
{
    public const string Google = "google";
    public const string Facebook = "facebook";
    public const string Apple = "apple";
}

public sealed record ExternalUserInfo(
    string ProviderUserId,
    string? Email,
    bool EmailVerified);

public enum ExternalTokenValidationFailureReason
{
    InvalidToken,
    ExpiredToken,
    WrongAudience,
    WrongIssuer,
    ProviderUnavailable
}

public sealed record ExternalTokenValidationResult(
    bool IsValid,
    ExternalUserInfo? UserInfo,
    ExternalTokenValidationFailureReason? FailureReason);

public interface IExternalIdentityProvider
{
    string ProviderName { get; }

    Task<ExternalTokenValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}

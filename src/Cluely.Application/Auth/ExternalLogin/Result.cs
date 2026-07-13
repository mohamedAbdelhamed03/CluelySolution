namespace Cluely.Application.Auth.ExternalLogin;

public sealed record ExternalLoginResult(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

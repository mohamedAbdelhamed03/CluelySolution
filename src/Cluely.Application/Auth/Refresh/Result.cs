namespace Cluely.Application.Auth.Refresh;

public sealed record RefreshTokenResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

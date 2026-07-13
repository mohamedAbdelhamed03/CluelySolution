namespace Cluely.Application.Common.Ports.Identity;

public sealed record AuthenticationSession(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public interface IAuthenticationSessionIssuer
{
    Task<AuthenticationSession> IssueAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default);
}

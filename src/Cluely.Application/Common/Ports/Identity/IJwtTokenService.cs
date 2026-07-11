namespace Cluely.Application.Common.Ports.Identity;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(Guid userId, string email);

    Guid? ValidateAccessToken(string token);
}

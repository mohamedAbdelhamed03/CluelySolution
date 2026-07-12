using Cluely.Application.Common.Ports.Identity;

namespace Cluely.Infrastructure.Identity.Security;

public sealed class AuthenticationSessionIssuer : IAuthenticationSessionIssuer
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenFactory _refreshTokenFactory;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthenticationSessionIssuer(
        IJwtTokenService jwtTokenService,
        IRefreshTokenFactory refreshTokenFactory,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _jwtTokenService = jwtTokenService;
        _refreshTokenFactory = refreshTokenFactory;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthenticationSession> IssueAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var accessToken = _jwtTokenService.CreateAccessToken(userId, email);
        var refreshToken = _refreshTokenFactory.Create(userId);
        await _refreshTokenRepository.CreateAsync(refreshToken.Record, cancellationToken);

        return new AuthenticationSession(
            userId,
            email,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.PlainTextToken,
            refreshToken.Record.ExpiresAt);
    }
}

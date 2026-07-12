using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cluely.Application.Auth.Refresh;

public sealed class RefreshTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenFactory _refreshTokenFactory;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IValidator<RefreshTokenCommand> _validator;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IRefreshTokenFactory refreshTokenFactory,
        IRefreshTokenHasher refreshTokenHasher,
        IValidator<RefreshTokenCommand> validator,
        ILogger<RefreshTokenHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _refreshTokenFactory = refreshTokenFactory;
        _refreshTokenHasher = refreshTokenHasher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResult>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<RefreshTokenResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var tokenHash = _refreshTokenHasher.Hash(command.RefreshToken);
        var existing = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existing is null
            || existing.RevokedAt is not null
            || existing.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Rejected refresh token request.");
            return Result.Failure<RefreshTokenResult>(new BusinessError(
                "InvalidRefreshToken",
                "Refresh token is invalid or expired."));
        }

        var user = await _userRepository.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null || user.AccountStatus != "Active")
        {
            return Result.Failure<RefreshTokenResult>(new BusinessError(
                "InvalidRefreshToken",
                "Refresh token is invalid or expired."));
        }

        var newRefreshToken = _refreshTokenFactory.Create(user.UserId);
        var rotated = await _refreshTokenRepository.RotateAsync(
            existing.Id,
            newRefreshToken.Record,
            cancellationToken);
        if (!rotated)
        {
            _logger.LogWarning("Rejected replayed refresh token request for user {UserId}.", user.UserId);
            return Result.Failure<RefreshTokenResult>(new BusinessError(
                "InvalidRefreshToken",
                "Refresh token is invalid or expired."));
        }

        var accessToken = _jwtTokenService.CreateAccessToken(user.UserId, user.Email);

        _logger.LogInformation("Refresh token rotated for user {UserId}.", user.UserId);

        return Result.Success(new RefreshTokenResult(
            accessToken.Token,
            accessToken.ExpiresAt,
            newRefreshToken.PlainTextToken,
            newRefreshToken.Record.ExpiresAt));
    }
}

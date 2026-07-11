using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cluely.Application.Auth.Logout;

public sealed class LogoutUserHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IValidator<LogoutUserCommand> _validator;
    private readonly ILogger<LogoutUserHandler> _logger;

    public LogoutUserHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenHasher refreshTokenHasher,
        IValidator<LogoutUserCommand> validator,
        ILogger<LogoutUserHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenHasher = refreshTokenHasher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<LogoutUserResult>> HandleAsync(
        LogoutUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<LogoutUserResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var tokenHash = _refreshTokenHasher.Hash(command.RefreshToken);
        var existing = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existing is not null && existing.RevokedAt is null)
        {
            await _refreshTokenRepository.RevokeAsync(existing.Id, replacedByTokenHash: null, cancellationToken);
            _logger.LogInformation("User {UserId} logged out.", existing.UserId);
        }

        return Result.Success(new LogoutUserResult());
    }
}

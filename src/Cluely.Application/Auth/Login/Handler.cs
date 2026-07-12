using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cluely.Application.Auth.Login;

public sealed class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenFactory _refreshTokenFactory;
    private readonly IValidator<LoginUserCommand> _validator;
    private readonly ILogger<LoginUserHandler> _logger;

    public LoginUserHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenFactory refreshTokenFactory,
        IValidator<LoginUserCommand> validator,
        ILogger<LoginUserHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenFactory = refreshTokenFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<LoginUserResult>> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<LoginUserResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            _logger.LogWarning("Rejected login attempt with invalid credentials.");
            return Result.Failure<LoginUserResult>(new BusinessError(
                "InvalidCredentials",
                "Invalid email or password."));
        }

        if (user.AccountStatus != "Active")
        {
            return Result.Failure<LoginUserResult>(new BusinessError(
                "AccountInactive",
                "Account is not active."));
        }

        var accessToken = _jwtTokenService.CreateAccessToken(user.UserId, user.Email);
        var refreshToken = _refreshTokenFactory.Create(user.UserId);
        await _refreshTokenRepository.CreateAsync(refreshToken.Record, cancellationToken);

        _logger.LogInformation("Successful login for user {UserId}.", user.UserId);

        return Result.Success(new LoginUserResult(
            user.UserId,
            user.Email,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.PlainTextToken,
            refreshToken.Record.ExpiresAt));
    }
}

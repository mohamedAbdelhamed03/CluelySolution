using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cluely.Application.Auth.Login;

public sealed class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthenticationSessionIssuer _sessionIssuer;
    private readonly IValidator<LoginUserCommand> _validator;
    private readonly ILogger<LoginUserHandler> _logger;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthenticationSessionIssuer sessionIssuer,
        IValidator<LoginUserCommand> validator,
        ILogger<LoginUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
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
        if (user is null
            || string.IsNullOrWhiteSpace(user.PasswordHash)
            || !_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
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

        var session = await _sessionIssuer.IssueAsync(user.UserId, user.Email, cancellationToken);
        _logger.LogInformation("Successful login for user {UserId}.", user.UserId);

        return Result.Success(new LoginUserResult(
            session.UserId,
            session.Email,
            session.AccessToken,
            session.AccessTokenExpiresAt,
            session.RefreshToken,
            session.RefreshTokenExpiresAt));
    }
}

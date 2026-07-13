using Cluely.Application.Auth.Common;
using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cluely.Application.Auth.ExternalLogin;

public sealed class ExternalLoginHandler
{
    private readonly IExternalIdentityProviderRegistry _providerRegistry;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuthenticationSessionIssuer _sessionIssuer;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IValidator<ExternalLoginCommand> _validator;
    private readonly ILogger<ExternalLoginHandler> _logger;

    public ExternalLoginHandler(
        IExternalIdentityProviderRegistry providerRegistry,
        IExternalLoginRepository externalLoginRepository,
        IUserRepository userRepository,
        IAuthenticationSessionIssuer sessionIssuer,
        IGuidGenerator guidGenerator,
        IValidator<ExternalLoginCommand> validator,
        ILogger<ExternalLoginHandler> logger)
    {
        _providerRegistry = providerRegistry;
        _externalLoginRepository = externalLoginRepository;
        _userRepository = userRepository;
        _sessionIssuer = sessionIssuer;
        _guidGenerator = guidGenerator;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ExternalLoginResult>> HandleAsync(
        ExternalLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<ExternalLoginResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var normalizedProvider = command.Provider.Trim().ToLowerInvariant();
        var provider = _providerRegistry.Resolve(normalizedProvider);
        if (provider is null)
        {
            return Result.Failure<ExternalLoginResult>(new BusinessError(
                "UnsupportedProvider",
                "The requested external provider is not supported."));
        }

        var tokenValidation = await provider.ValidateTokenAsync(command.Token, cancellationToken);
        if (!tokenValidation.IsValid || tokenValidation.UserInfo is null)
        {
            return ExternalAuthFailureMapper.MapValidationFailure<ExternalLoginResult>(
                tokenValidation.FailureReason
                    ?? ExternalTokenValidationFailureReason.InvalidToken);
        }

        var providerUserInfo = tokenValidation.UserInfo;
        var existingLogin = await _externalLoginRepository.GetByProviderUserAsync(
            normalizedProvider,
            providerUserInfo.ProviderUserId,
            cancellationToken);

        if (existingLogin is not null)
        {
            return await IssueSessionForExistingUserAsync(existingLogin.UserId, cancellationToken);
        }

        return await RegisterAndIssueSessionAsync(
            normalizedProvider,
            providerUserInfo,
            cancellationToken);
    }

    private async Task<Result<ExternalLoginResult>> IssueSessionForExistingUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<ExternalLoginResult>(new BusinessError(
                "UserNotFound",
                "User account was not found."));
        }

        if (user.AccountStatus != "Active")
        {
            return Result.Failure<ExternalLoginResult>(new BusinessError(
                "AccountInactive",
                "Account is not active."));
        }

        var session = await _sessionIssuer.IssueAsync(user.UserId, user.Email, cancellationToken);
        _logger.LogInformation("Successful external login for user {UserId}.", user.UserId);

        return Result.Success(Map(session));
    }

    private async Task<Result<ExternalLoginResult>> RegisterAndIssueSessionAsync(
        string provider,
        ExternalUserInfo providerUserInfo,
        CancellationToken cancellationToken)
    {
        var email = ResolveAccountEmail(provider, providerUserInfo);
        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            return Result.Failure<ExternalLoginResult>(new BusinessError(
                "DuplicateEmail",
                "An account with this email already exists."));
        }

        var userId = _guidGenerator.Generate();
        var user = new UserAccount(
            userId,
            email,
            PasswordHash: null,
            AccountStatus: "Active",
            CreatedAt: DateTime.UtcNow);

        var externalLogin = new ExternalLoginAccount(
            _guidGenerator.Generate(),
            userId,
            provider,
            providerUserInfo.ProviderUserId,
            NormalizeEmail(providerUserInfo.Email),
            providerUserInfo.EmailVerified,
            DateTime.UtcNow);

        await _userRepository.CreateAsync(user, cancellationToken);
        await _externalLoginRepository.CreateAsync(externalLogin, cancellationToken);

        var session = await _sessionIssuer.IssueAsync(userId, email, cancellationToken);
        _logger.LogInformation("Registered user {UserId} via external provider {Provider}.", userId, provider);

        return Result.Success(Map(session));
    }

    private static string ResolveAccountEmail(string provider, ExternalUserInfo providerUserInfo)
    {
        if (providerUserInfo.EmailVerified)
        {
            var verifiedEmail = NormalizeEmail(providerUserInfo.Email);
            if (!string.IsNullOrWhiteSpace(verifiedEmail))
            {
                return verifiedEmail!;
            }
        }

        return $"external+{provider}+{providerUserInfo.ProviderUserId}@cluely.local";
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static ExternalLoginResult Map(AuthenticationSession session)
        => new(
            session.UserId,
            session.Email,
            session.AccessToken,
            session.AccessTokenExpiresAt,
            session.RefreshToken,
            session.RefreshTokenExpiresAt);
}

using Cluely.Application.Auth.Common;
using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cluely.Application.Auth.LinkExternalLogin;

public sealed class LinkExternalLoginHandler
{
    private readonly IExternalIdentityProviderRegistry _providerRegistry;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IValidator<LinkExternalLoginCommand> _validator;
    private readonly ILogger<LinkExternalLoginHandler> _logger;

    public LinkExternalLoginHandler(
        IExternalIdentityProviderRegistry providerRegistry,
        IExternalLoginRepository externalLoginRepository,
        IUserRepository userRepository,
        IGuidGenerator guidGenerator,
        IValidator<LinkExternalLoginCommand> validator,
        ILogger<LinkExternalLoginHandler> logger)
    {
        _providerRegistry = providerRegistry;
        _externalLoginRepository = externalLoginRepository;
        _userRepository = userRepository;
        _guidGenerator = guidGenerator;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<LinkExternalLoginResult>> HandleAsync(
        LinkExternalLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<LinkExternalLoginResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<LinkExternalLoginResult>(new BusinessError(
                "UserNotFound",
                "User account was not found."));
        }

        if (user.AccountStatus != "Active")
        {
            return Result.Failure<LinkExternalLoginResult>(new BusinessError(
                "AccountInactive",
                "Account is not active."));
        }

        var normalizedProvider = command.Provider.Trim().ToLowerInvariant();
        if (await _externalLoginRepository.ExistsForUserAsync(command.UserId, normalizedProvider, cancellationToken))
        {
            return Result.Failure<LinkExternalLoginResult>(new BusinessError(
                "DuplicateProviderLink",
                "This provider is already linked to the account."));
        }

        var provider = _providerRegistry.Resolve(normalizedProvider);
        if (provider is null)
        {
            return Result.Failure<LinkExternalLoginResult>(new BusinessError(
                "UnsupportedProvider",
                "The requested external provider is not supported."));
        }

        var tokenValidation = await provider.ValidateTokenAsync(command.Token, cancellationToken);
        if (!tokenValidation.IsValid || tokenValidation.UserInfo is null)
        {
            return ExternalAuthFailureMapper.MapValidationFailure<LinkExternalLoginResult>(
                tokenValidation.FailureReason
                    ?? ExternalTokenValidationFailureReason.InvalidToken);
        }

        var providerUserInfo = tokenValidation.UserInfo;
        var existingLogin = await _externalLoginRepository.GetByProviderUserAsync(
            normalizedProvider,
            providerUserInfo.ProviderUserId,
            cancellationToken);

        if (existingLogin is not null && existingLogin.UserId != command.UserId)
        {
            return Result.Failure<LinkExternalLoginResult>(new BusinessError(
                "ProviderAccountAlreadyLinked",
                "This provider account is already linked to another user."));
        }

        if (existingLogin is not null)
        {
            return Result.Success(new LinkExternalLoginResult(normalizedProvider));
        }

        var externalLogin = new ExternalLoginAccount(
            _guidGenerator.Generate(),
            command.UserId,
            normalizedProvider,
            providerUserInfo.ProviderUserId,
            NormalizeEmail(providerUserInfo.Email),
            providerUserInfo.EmailVerified,
            DateTime.UtcNow);

        await _externalLoginRepository.CreateAsync(externalLogin, cancellationToken);
        _logger.LogInformation(
            "Linked external provider {Provider} to user {UserId}.",
            normalizedProvider,
            command.UserId);

        return Result.Success(new LinkExternalLoginResult(normalizedProvider));
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}

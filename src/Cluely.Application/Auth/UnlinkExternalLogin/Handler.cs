using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cluely.Application.Auth.UnlinkExternalLogin;

public sealed class UnlinkExternalLoginHandler
{
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly IValidator<UnlinkExternalLoginCommand> _validator;
    private readonly ILogger<UnlinkExternalLoginHandler> _logger;

    public UnlinkExternalLoginHandler(
        IExternalLoginRepository externalLoginRepository,
        IUserRepository userRepository,
        IValidator<UnlinkExternalLoginCommand> validator,
        ILogger<UnlinkExternalLoginHandler> logger)
    {
        _externalLoginRepository = externalLoginRepository;
        _userRepository = userRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<UnlinkExternalLoginResult>> HandleAsync(
        UnlinkExternalLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<UnlinkExternalLoginResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UnlinkExternalLoginResult>(new BusinessError(
                "UserNotFound",
                "User account was not found."));
        }

        var normalizedProvider = command.Provider.Trim().ToLowerInvariant();
        if (!await _externalLoginRepository.ExistsForUserAsync(command.UserId, normalizedProvider, cancellationToken))
        {
            return Result.Failure<UnlinkExternalLoginResult>(new BusinessError(
                "ExternalLoginNotFound",
                "The requested provider is not linked to this account."));
        }

        var externalLogins = await _externalLoginRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var hasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash);
        var remainingMethods = (hasPassword ? 1 : 0) + externalLogins.Count - 1;
        if (remainingMethods < 1)
        {
            return Result.Failure<UnlinkExternalLoginResult>(new BusinessError(
                "LastLoginMethodCannotBeRemoved",
                "At least one authentication method must remain on the account."));
        }

        await _externalLoginRepository.DeleteAsync(command.UserId, normalizedProvider, cancellationToken);
        _logger.LogInformation(
            "Unlinked external provider {Provider} from user {UserId}.",
            normalizedProvider,
            command.UserId);

        return Result.Success(new UnlinkExternalLoginResult(normalizedProvider));
    }
}

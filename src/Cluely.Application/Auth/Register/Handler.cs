using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentValidation;

namespace Cluely.Application.Auth.Register;

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IValidator<RegisterUserCommand> _validator;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IGuidGenerator guidGenerator,
        IValidator<RegisterUserCommand> validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _guidGenerator = guidGenerator;
        _validator = validator;
    }

    public async Task<Result<RegisterUserResult>> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<RegisterUserResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return Result.Failure<RegisterUserResult>(new BusinessError(
                "DuplicateEmail",
                "An account with this email already exists."));
        }

        var userId = _guidGenerator.Generate();
        var user = new UserAccount(
            userId,
            normalizedEmail,
            _passwordHasher.HashPassword(command.Password),
            AccountStatus: "Active",
            CreatedAt: DateTime.UtcNow);

        await _userRepository.CreateAsync(user, cancellationToken);

        return Result.Success(new RegisterUserResult(userId, normalizedEmail));
    }
}

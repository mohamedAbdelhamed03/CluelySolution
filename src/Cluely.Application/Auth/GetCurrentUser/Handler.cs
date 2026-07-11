using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentValidation;

namespace Cluely.Application.Auth.GetCurrentUser;

public sealed class GetCurrentUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<GetCurrentUserQuery> _validator;

    public GetCurrentUserHandler(
        IUserRepository userRepository,
        IValidator<GetCurrentUserQuery> validator)
    {
        _userRepository = userRepository;
        _validator = validator;
    }

    public async Task<Result<GetCurrentUserResult>> HandleAsync(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<GetCurrentUserResult>(new ValidationError(
                "ValidationFailed",
                "One or more validation errors occurred.",
                validationResult.ToDictionary()));
        }

        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<GetCurrentUserResult>(new BusinessError(
                "UserNotFound",
                "User not found."));
        }

        return Result.Success(new GetCurrentUserResult(user.UserId, user.Email, user.AccountStatus));
    }
}

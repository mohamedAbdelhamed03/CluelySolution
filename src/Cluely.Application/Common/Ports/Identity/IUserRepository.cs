namespace Cluely.Application.Common.Ports.Identity;

public sealed record UserAccount(
    Guid UserId,
    string Email,
    string? PasswordHash,
    string AccountStatus,
    DateTime CreatedAt);

public interface IUserRepository
{
    Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task CreateAsync(UserAccount user, CancellationToken cancellationToken = default);
}

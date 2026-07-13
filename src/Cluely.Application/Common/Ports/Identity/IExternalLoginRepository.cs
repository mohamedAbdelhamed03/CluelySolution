namespace Cluely.Application.Common.Ports.Identity;

public sealed record ExternalLoginAccount(
    Guid ExternalLoginId,
    Guid UserId,
    string Provider,
    string ProviderUserId,
    string? Email,
    bool EmailVerified,
    DateTime CreatedAt);

public interface IExternalLoginRepository
{
    Task<ExternalLoginAccount?> GetByProviderUserAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalLoginAccount>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForUserAsync(
        Guid userId,
        string provider,
        CancellationToken cancellationToken = default);

    Task CreateAsync(ExternalLoginAccount account, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
}

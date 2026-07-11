namespace Cluely.Application.Common.Ports.Identity;

public sealed record RefreshTokenRecord(
    Guid Id,
    Guid UserId,
    string TokenHash,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime? RevokedAt,
    string? ReplacedByTokenHash);

public interface IRefreshTokenRepository
{
    Task<RefreshTokenRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task CreateAsync(RefreshTokenRecord token, CancellationToken cancellationToken = default);

    Task RevokeAsync(Guid tokenId, string? replacedByTokenHash, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

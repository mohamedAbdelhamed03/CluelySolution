using Cluely.Application.Common.Ports.Identity;
using Cluely.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Cluely.Infrastructure.Identity.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public RefreshTokenRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshTokenRecord?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task CreateAsync(RefreshTokenRecord token, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = token.Id,
            UserId = token.UserId,
            TokenHash = token.TokenHash,
            ExpiresAt = token.ExpiresAt,
            CreatedAt = token.CreatedAt,
            RevokedAt = token.RevokedAt,
            ReplacedByTokenHash = token.ReplacedByTokenHash,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(
        Guid tokenId,
        string? replacedByTokenHash,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.Id == tokenId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.RevokedAt = DateTime.UtcNow;
        entity.ReplacedByTokenHash = replacedByTokenHash;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RotateAsync(
        Guid currentTokenId,
        RefreshTokenRecord replacement,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var revokedAt = DateTime.UtcNow;
        var affectedRows = await _dbContext.RefreshTokens
            .Where(token => token.Id == currentTokenId
                && token.RevokedAt == null
                && token.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, revokedAt)
                    .SetProperty(token => token.ReplacedByTokenHash, replacement.TokenHash),
                cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        _dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = replacement.Id,
            UserId = replacement.UserId,
            TokenHash = replacement.TokenHash,
            ExpiresAt = replacement.ExpiresAt,
            CreatedAt = replacement.CreatedAt,
            RevokedAt = replacement.RevokedAt,
            ReplacedByTokenHash = replacement.ReplacedByTokenHash,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RefreshTokenRecord Map(RefreshTokenEntity entity)
        => new(
            entity.Id,
            entity.UserId,
            entity.TokenHash,
            entity.ExpiresAt,
            entity.CreatedAt,
            entity.RevokedAt,
            entity.ReplacedByTokenHash);
}

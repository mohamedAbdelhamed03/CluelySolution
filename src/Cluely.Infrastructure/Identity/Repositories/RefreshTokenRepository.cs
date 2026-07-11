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

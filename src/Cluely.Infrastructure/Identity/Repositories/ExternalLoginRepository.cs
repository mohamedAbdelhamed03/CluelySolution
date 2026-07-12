using Cluely.Application.Common.Ports.Identity;
using Cluely.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Cluely.Infrastructure.Identity.Repositories;

public sealed class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly IdentityDbContext _dbContext;

    public ExternalLoginRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExternalLoginAccount?> GetByProviderUserAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ExternalLogins.AsNoTracking()
            .FirstOrDefaultAsync(
                login => login.Provider == provider && login.ProviderUserId == providerUserId,
                cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<ExternalLoginAccount>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.ExternalLogins.AsNoTracking()
            .Where(login => login.UserId == userId)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToList();
    }

    public Task<bool> ExistsForUserAsync(
        Guid userId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExternalLogins.AnyAsync(
            login => login.UserId == userId && login.Provider == provider,
            cancellationToken);
    }

    public async Task CreateAsync(ExternalLoginAccount account, CancellationToken cancellationToken = default)
    {
        _dbContext.ExternalLogins.Add(new ExternalLoginEntity
        {
            ExternalLoginId = account.ExternalLoginId,
            UserId = account.UserId,
            Provider = account.Provider,
            ProviderUserId = account.ProviderUserId,
            Email = account.Email,
            EmailVerified = account.EmailVerified,
            CreatedAt = account.CreatedAt,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, string provider, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ExternalLogins
            .FirstOrDefaultAsync(
                login => login.UserId == userId && login.Provider == provider,
                cancellationToken);

        if (entity is null)
        {
            return;
        }

        _dbContext.ExternalLogins.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ExternalLoginAccount Map(ExternalLoginEntity entity)
        => new(
            entity.ExternalLoginId,
            entity.UserId,
            entity.Provider,
            entity.ProviderUserId,
            entity.Email,
            entity.EmailVerified,
            entity.CreatedAt);
}

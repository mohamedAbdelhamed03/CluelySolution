using Cluely.Application.Common.Ports.Identity;
using Cluely.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Cluely.Infrastructure.Identity.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(user => user.UserId == userId, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task CreateAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(new UserEntity
        {
            UserId = user.UserId,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            AccountStatus = user.AccountStatus,
            CreatedAt = user.CreatedAt,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static UserAccount Map(UserEntity entity)
        => new(entity.UserId, entity.Email, entity.PasswordHash, entity.AccountStatus, entity.CreatedAt);
}

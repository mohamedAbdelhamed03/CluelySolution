using Cluely.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Cluely.Infrastructure.Identity;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<ParticipantBindingEntity> ParticipantBindings => Set<ParticipantBindingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.RefreshTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ParticipantBindingEntityConfiguration());
    }
}

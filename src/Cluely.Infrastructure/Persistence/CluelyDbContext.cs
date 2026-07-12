using Cluely.Infrastructure.Persistence.Configurations;
using Cluely.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Cluely.Infrastructure.Persistence;

public class CluelyDbContext : DbContext
{
    public CluelyDbContext(DbContextOptions<CluelyDbContext> options) : base(options)
    {
    }

    public DbSet<RoomSnapshot> RoomSnapshots { get; set; }
    public DbSet<RoomEvent> RoomEvents { get; set; }
    public DbSet<DictionarySnapshot> DictionarySnapshots { get; set; }
    public DbSet<DictionaryShareGrantRow> DictionaryShareGrants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoomSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new RoomEventConfiguration());
        modelBuilder.ApplyConfiguration(new DictionarySnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new DictionaryShareGrantConfiguration());
    }
}

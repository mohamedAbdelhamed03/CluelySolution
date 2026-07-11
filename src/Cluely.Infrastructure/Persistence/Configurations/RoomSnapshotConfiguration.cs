using Cluely.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cluely.Infrastructure.Persistence.Configurations;

public class RoomSnapshotConfiguration : IEntityTypeConfiguration<RoomSnapshot>
{
    public void Configure(EntityTypeBuilder<RoomSnapshot> builder)
    {
        builder.ToTable("RoomSnapshots");
        builder.HasKey(rs => rs.RoomId);
        builder.HasIndex(rs => rs.RoomCode).IsUnique();
        builder.Property(rs => rs.RoomCode).HasMaxLength(32).IsRequired();
        builder.Property(rs => rs.SerializedState).IsRequired();
        builder.Property(rs => rs.SnapshotSchemaVersion).HasDefaultValue(1);
    }
}

public class RoomEventConfiguration : IEntityTypeConfiguration<RoomEvent>
{
    public void Configure(EntityTypeBuilder<RoomEvent> builder)
    {
        builder.ToTable("RoomEvents");
        builder.HasKey(re => re.Id);
        builder.HasIndex(re => new { re.RoomId, re.Sequence }).IsUnique();
        builder.HasIndex(re => re.RoomId);
        builder.Property(re => re.EventType).IsRequired();
        builder.Property(re => re.EventData).IsRequired();
    }
}

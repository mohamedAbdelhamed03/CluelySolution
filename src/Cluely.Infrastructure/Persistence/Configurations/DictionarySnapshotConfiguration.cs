using Cluely.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cluely.Infrastructure.Persistence.Configurations;

public class DictionarySnapshotConfiguration : IEntityTypeConfiguration<DictionarySnapshot>
{
    public void Configure(EntityTypeBuilder<DictionarySnapshot> builder)
    {
        builder.ToTable("DictionarySnapshots");
        builder.HasKey(d => d.DictionaryId);

        builder.Property(d => d.OwnerId).IsRequired();
        builder.HasIndex(d => d.OwnerId);
        builder.HasIndex(d => d.Visibility);

        builder.Property(d => d.ContentType).HasMaxLength(32).IsRequired();
        builder.Property(d => d.Visibility).HasMaxLength(32).IsRequired();
        builder.Property(d => d.State).HasMaxLength(32).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(256).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(4096).IsRequired();
        builder.Property(d => d.Language).HasMaxLength(32).IsRequired();
        builder.Property(d => d.Region).HasMaxLength(32);
        builder.Property(d => d.TagsJson).IsRequired();
        builder.Property(d => d.SerializedState).IsRequired();
        builder.Property(d => d.SnapshotSchemaVersion).HasDefaultValue(1);

        // AggregateVersion is the optimistic-concurrency token.
        builder.Property(d => d.Version).IsConcurrencyToken();

        // Idempotency key is unique when present (deterministic create/clone replay).
        builder.HasIndex(d => d.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasMany(d => d.ShareGrants)
            .WithOne()
            .HasForeignKey(g => g.DictionaryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DictionaryShareGrantConfiguration : IEntityTypeConfiguration<DictionaryShareGrantRow>
{
    public void Configure(EntityTypeBuilder<DictionaryShareGrantRow> builder)
    {
        builder.ToTable("DictionaryShareGrants");
        builder.HasKey(g => new { g.DictionaryId, g.GranteeId });
        builder.HasIndex(g => g.GranteeId);
    }
}

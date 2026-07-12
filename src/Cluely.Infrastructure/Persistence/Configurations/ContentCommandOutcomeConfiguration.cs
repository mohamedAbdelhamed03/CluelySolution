using Cluely.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cluely.Infrastructure.Persistence.Configurations;

internal sealed class ContentCommandOutcomeConfiguration : IEntityTypeConfiguration<ContentCommandOutcome>
{
    public void Configure(EntityTypeBuilder<ContentCommandOutcome> builder)
    {
        builder.ToTable("ContentCommandOutcomes");

        builder.HasKey(outcome => outcome.IdempotencyKey);

        builder.Property(outcome => outcome.CommandName)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(outcome => new { outcome.DictionaryId, outcome.CommandName });
    }
}

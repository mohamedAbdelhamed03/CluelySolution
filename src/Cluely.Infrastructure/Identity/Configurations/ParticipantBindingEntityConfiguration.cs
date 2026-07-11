using Cluely.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cluely.Infrastructure.Identity.Configurations;

internal sealed class ParticipantBindingEntityConfiguration : IEntityTypeConfiguration<ParticipantBindingEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantBindingEntity> builder)
    {
        builder.ToTable("ParticipantBindings");
        builder.HasKey(binding => new { binding.UserId, binding.RoomId });
        builder.Property(binding => binding.ParticipantId).IsRequired();
        builder.HasIndex(binding => new { binding.RoomId, binding.ParticipantId });
    }
}

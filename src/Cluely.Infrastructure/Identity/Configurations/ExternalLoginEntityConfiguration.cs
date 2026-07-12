using Cluely.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cluely.Infrastructure.Identity.Configurations;

internal sealed class ExternalLoginEntityConfiguration : IEntityTypeConfiguration<ExternalLoginEntity>
{
    public void Configure(EntityTypeBuilder<ExternalLoginEntity> builder)
    {
        builder.ToTable("ExternalLogins");
        builder.HasKey(login => login.ExternalLoginId);
        builder.Property(login => login.Provider).HasMaxLength(32).IsRequired();
        builder.Property(login => login.ProviderUserId).HasMaxLength(256).IsRequired();
        builder.Property(login => login.Email).HasMaxLength(256);
        builder.Property(login => login.CreatedAt).IsRequired();
        builder.HasIndex(login => new { login.Provider, login.ProviderUserId }).IsUnique();
        builder.HasIndex(login => new { login.UserId, login.Provider }).IsUnique();
        builder.HasOne(login => login.User)
            .WithMany()
            .HasForeignKey(login => login.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

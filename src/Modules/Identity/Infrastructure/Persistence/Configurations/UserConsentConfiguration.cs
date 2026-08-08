using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ConsentType).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.UserId);

        builder.Property(c => c.ConsentedAtUtc).IsRequired();

        // Restrict, not Cascade: consent history is a compliance record and must never be
        // silently lost as a side effect of an unrelated user-deletion cascade.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

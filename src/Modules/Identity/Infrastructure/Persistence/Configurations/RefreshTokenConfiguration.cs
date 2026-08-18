using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.FamilyId);

        builder.Property(t => t.IssuedAtUtc).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).IsRequired();

        // Optimistic concurrency (DEVELOPMENT_INSTRUCTIONS.md §4): two concurrent /auth/refresh
        // calls presenting the SAME still-active token both read RevokedAtUtc == null before
        // either commits. Without this, both would pass the reuse check and both rotations would
        // succeed, silently branching two active sessions from one token instead of the intended
        // single-rotation invariant. Marking it a concurrency token adds RevokedAtUtc to the
        // UPDATE's WHERE clause, so the loser's write matches zero rows and EF Core throws
        // DbUpdateConcurrencyException instead of silently overwriting — see RefreshTokenHandler.
        builder.Property(t => t.RevokedAtUtc).IsConcurrencyToken();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

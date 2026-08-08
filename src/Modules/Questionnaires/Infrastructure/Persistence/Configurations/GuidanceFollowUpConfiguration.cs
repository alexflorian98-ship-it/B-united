using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;

public sealed class GuidanceFollowUpConfiguration : IEntityTypeConfiguration<GuidanceFollowUp>
{
    public void Configure(EntityTypeBuilder<GuidanceFollowUp> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Question).IsRequired();
        builder.Property(f => f.Answer);
        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.UpdatedAt).IsRequired();

        // Enforces "one bounded follow-up per guidance" at the database layer.
        builder.HasIndex(f => f.GuidanceResponseId).IsUnique();
        builder.HasOne<GuidanceResponse>().WithMany().HasForeignKey(f => f.GuidanceResponseId).OnDelete(DeleteBehavior.Cascade);
    }
}

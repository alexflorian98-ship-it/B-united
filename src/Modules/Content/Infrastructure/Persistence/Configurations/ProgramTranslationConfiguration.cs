using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Infrastructure.Persistence.Configurations;

public sealed class ProgramTranslationConfiguration : IEntityTypeConfiguration<ProgramTranslation>
{
    public void Configure(EntityTypeBuilder<ProgramTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.ShortDescription).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Description).IsRequired();

        builder.HasIndex(t => new { t.ProgramId, t.Language }).IsUnique();
        builder.HasOne<Program>().WithMany().HasForeignKey(t => t.ProgramId).OnDelete(DeleteBehavior.Cascade);
    }
}

using BUnited.Modules.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Events.Infrastructure.Persistence.Configurations;

public sealed class EventProgramConfiguration : IEntityTypeConfiguration<EventProgram>
{
    public void Configure(EntityTypeBuilder<EventProgram> builder)
    {
        builder.HasKey(ep => new { ep.EventId, ep.ProgramId });

        builder.HasIndex(ep => ep.ProgramId);

        builder.HasOne<Event>().WithMany().HasForeignKey(ep => ep.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

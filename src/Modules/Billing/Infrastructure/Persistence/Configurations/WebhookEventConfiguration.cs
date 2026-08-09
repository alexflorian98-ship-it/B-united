using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProviderEventId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PayloadJson).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // Idempotent processing (P3.08) hinges entirely on this constraint.
        builder.HasIndex(e => e.ProviderEventId).IsUnique();
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.PurchaseId);
    }
}

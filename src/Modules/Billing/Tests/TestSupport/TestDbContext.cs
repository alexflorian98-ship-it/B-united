using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Tests.TestSupport;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : BUnitedDbContext(options, [typeof(PlanConfiguration).Assembly])
{
    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<PlanPrice> PlanPrices => Set<PlanPrice>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<SubscriptionPeriod> SubscriptionPeriods => Set<SubscriptionPeriod>();

    public DbSet<PaymentCustomer> PaymentCustomers => Set<PaymentCustomer>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

    public DbSet<Entitlement> Entitlements => Set<Entitlement>();
}

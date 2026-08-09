using BUnited.Modules.Admin.Application.UseCases;
using BUnited.Modules.Admin.Tests.TestSupport;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Admin.Tests.Application;

/// <summary>docs/IMPLEMENTATION_PLAN.md Slice A3 — the purchases/entitlements section of the
/// client detail screen. Proves the ADR-007 cross-module read is scoped to exactly one user and
/// never mutates Billing's tables.</summary>
public sealed class GetClientCommerceSummaryHandlerTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<(TestDbContext DbContext, IDisposable Connection, Guid TargetUserId, Guid OtherUserId, string ProgramSlug)> SeedAsync()
    {
        var (connection, dbContext) = TestDbContextFactory.Create();

        var domain = ContentDomain.Create(Guid.NewGuid(), "wellbeing", 0);
        var program = Program.Create(domain.Id, "resilience-101", "ro", Actor);
        program.Publish(Actor);
        dbContext.AddRange(domain, program);

        var offer = ProgramOffer.Create(program.Id);
        var price = ProgramPrice.Create(offer.Id, 100m, "RON", Now.AddDays(-30));
        dbContext.AddRange(offer, price);

        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var targetPurchase = Purchase.Create(targetUserId, program.Id, offer.Id, price.Id, 100m, "RON");
        targetPurchase.MarkSucceeded(Now.AddDays(-2));
        var otherPurchase = Purchase.Create(otherUserId, program.Id, offer.Id, price.Id, 100m, "RON");
        otherPurchase.MarkSucceeded(Now.AddDays(-2));
        dbContext.AddRange(targetPurchase, otherPurchase);

        var targetEntitlement = ProgramEntitlement.Grant(targetUserId, program.Id, targetPurchase.Id, Now.AddDays(-2));
        var otherEntitlement = ProgramEntitlement.Grant(otherUserId, program.Id, otherPurchase.Id, Now.AddDays(-2));
        dbContext.AddRange(targetEntitlement, otherEntitlement);

        await dbContext.SaveChangesAsync();

        return (dbContext, connection, targetUserId, otherUserId, program.Slug);
    }

    [Fact]
    public async Task Returns_only_the_requested_users_purchases_and_entitlements_with_program_slugs()
    {
        var (dbContext, connection, targetUserId, _, programSlug) = await SeedAsync();
        using var _ = connection;

        var result = await new GetClientCommerceSummaryHandler(dbContext).HandleAsync(targetUserId, CancellationToken.None);

        var purchase = Assert.Single(result.Purchases);
        Assert.Equal(programSlug, purchase.ProgramSlug);
        Assert.Equal("Succeeded", purchase.Status);

        var entitlement = Assert.Single(result.Entitlements);
        Assert.Equal(programSlug, entitlement.ProgramSlug);
        Assert.Equal("Active", entitlement.Status);
    }

    [Fact]
    public async Task Never_writes_to_any_row_it_reads()
    {
        var (dbContext, connection, targetUserId, _, _) = await SeedAsync();
        using var _ = connection;

        var purchaseCountBefore = await dbContext.Purchases.CountAsync();
        var entitlementCountBefore = await dbContext.ProgramEntitlements.CountAsync();

        await new GetClientCommerceSummaryHandler(dbContext).HandleAsync(targetUserId, CancellationToken.None);

        Assert.DoesNotContain(dbContext.ChangeTracker.Entries(), e => e.State != EntityState.Unchanged && e.State != EntityState.Detached);
        Assert.Equal(purchaseCountBefore, await dbContext.Purchases.CountAsync());
        Assert.Equal(entitlementCountBefore, await dbContext.ProgramEntitlements.CountAsync());
    }
}

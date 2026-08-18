using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Content.Domain.Entities;
using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Progress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace BUnited.Migrations.Seed;

/// <summary>docs/TASKS.md P7.18.b — representative demo Client/Expert accounts plus a completed
/// purchase and in-progress content state, so a presenter/tester never has to author this by
/// hand. Idempotent (keyed off fixed demo emails), and — like `admin@bunited.local` — created
/// through the same <see cref="IPasswordHasher"/> the real login path verifies against, never a
/// raw insert that bypasses hashing. Depends on <see cref="DemoProgramSeeder"/> having already
/// run in the same startup pass (skips itself if the demo program isn't there yet, same
/// defensive pattern as <see cref="ProgramOfferSeeder"/>).</summary>
public static class DemoAccountSeeder
{
    private const string ClientEmail = "demo.client@bunited.local";
    private const string ExpertEmail = "demo.expert@bunited.local";

    /// <summary>Documented here and in README.md's demo section — a fixed, non-secret local-dev
    /// password is acceptable for a seeded demo account: it never runs in Production (P3.32's
    /// startup gate refuses to boot there with any demo-only adapter registered), and this
    /// seeder itself has no gate of its own to bypass since it grants no elevated access beyond
    /// what a real self-registration would.</summary>
    private const string DemoPassword = "DemoAccount123!";

    public static async Task SeedAsync(BUnitedApplicationDbContext context, IPasswordHasher passwordHasher, IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (environment.IsProduction())
        {
            // A well-known demo password must never exist in Production — unlike P3.32's
            // demo-only *adapter* gate (which throws and refuses to boot), seeded demo *data* is
            // optional, so silently skipping is correct here: the app must still start cleanly in
            // Production without it.
            return;
        }

        if (await context.Users.AnyAsync(u => u.NormalizedEmail == User.Normalize(ClientEmail), cancellationToken))
        {
            return;
        }

        var program = await context.Programs.SingleOrDefaultAsync(p => p.Slug == "mindful-living", cancellationToken);
        if (program is null)
        {
            // DemoProgramSeeder hasn't run yet (or was removed) — nothing to grant access to.
            return;
        }

        var utcNow = DateTime.UtcNow;

        var client = User.Register(ClientEmail, passwordHasher.Hash(DemoPassword));
        client.MarkEmailVerified(utcNow);
        client.AssignRole(WellKnownRoles.ClientId);
        context.Users.Add(client);
        context.Set<UserPreference>().Add(UserPreference.CreateDefault(client.Id));

        var expert = User.Register(ExpertEmail, passwordHasher.Hash(DemoPassword));
        expert.MarkEmailVerified(utcNow);
        expert.AssignRole(WellKnownRoles.ExpertId);
        context.Users.Add(expert);
        context.Set<UserPreference>().Add(UserPreference.CreateDefault(expert.Id));

        var offer = await context.Set<ProgramOffer>().SingleOrDefaultAsync(o => o.ProgramId == program.Id && o.Status == ProgramOfferStatus.Active, cancellationToken);
        ProgramPrice price;
        if (offer is null)
        {
            offer = ProgramOffer.Create(program.Id);
            offer.Activate();
            context.Set<ProgramOffer>().Add(offer);

            price = ProgramPrice.Create(offer.Id, 299.00m, "RON", utcNow);
            context.Set<ProgramPrice>().Add(price);
        }
        else
        {
            price = await context.Set<ProgramPrice>()
                .Where(p => p.ProgramOfferId == offer.Id)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstAsync(cancellationToken);
        }

        var programTitle = await context.ProgramTranslations
            .Where(t => t.ProgramId == program.Id && t.Language == program.DefaultLanguage)
            .Select(t => t.Title)
            .SingleAsync(cancellationToken);

        var purchase = Purchase.Create(client.Id, program.Id, offer.Id, price.Id, price.Amount, price.Currency, programTitle);
        purchase.MarkSucceeded(utcNow);
        context.Purchases.Add(purchase);

        var payment = Payment.Create(purchase.Id, price.Amount, price.Currency, PaymentStatus.Succeeded, providerPaymentId: $"demo_{purchase.Id:N}", utcNow);
        context.Payments.Add(payment);

        context.Invoices.Add(Invoice.Create(purchase.Id, payment.Id, price.Amount, price.Currency, InvoiceStatus.Paid, utcNow));

        context.Set<ProgramEntitlement>().Add(ProgramEntitlement.Grant(client.Id, program.Id, purchase.Id, utcNow));

        // A little visible progress: the intro video half-watched, the first reading completed —
        // enough for a demo to show both an in-progress and a completed content item without
        // requiring a live click-through first.
        var introSection = await context.Sections.Where(s => s.ProgramId == program.Id).OrderBy(s => s.SortOrder).FirstAsync(cancellationToken);
        var introItems = await context.ContentItems.Where(i => i.SectionId == introSection.Id).OrderBy(i => i.SortOrder).ToListAsync(cancellationToken);
        var videoItem = introItems.Single(i => i.Type == BUnited.Modules.Content.Domain.ContentItemType.Video);
        var readingItem = introItems.Single(i => i.Type == BUnited.Modules.Content.Domain.ContentItemType.RichText);

        var videoProgress = ContentProgress.Start(client.Id, videoItem.Id);
        videoProgress.RecordVideoPosition(positionSeconds: 90, watchPercentage: 45.0, utcNow);
        context.ContentProgressEntries.Add(videoProgress);

        var readingProgress = ContentProgress.Start(client.Id, readingItem.Id);
        readingProgress.MarkCompletedManually(utcNow);
        context.ContentProgressEntries.Add(readingProgress);

        await context.SaveChangesAsync(cancellationToken);
    }
}

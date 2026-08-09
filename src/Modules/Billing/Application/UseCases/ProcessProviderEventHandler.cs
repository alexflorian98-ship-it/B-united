using System.Linq;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

/// <summary>The single choke point every provider event (fake, for V1 — ADR-010) passes
/// through, whether it arrived via the demo webhook HTTP endpoint or was generated in-process by
/// checkout/demo-action handlers. Owns idempotency (P3.08), out-of-order safety (P3.09), the
/// state-machine transitions (P3.10), the resulting <see cref="ProgramEntitlement"/>
/// grant/revoke (P3.38/39), and the audit trail (P3.12) — every caller gets all of that for free
/// rather than re-implementing it. No outbox: <see cref="ProgramEntitlement"/> granting stays
/// synchronous within the same transaction as marking a <see cref="Purchase"/> succeeded.</summary>
public sealed class ProcessProviderEventHandler(DbContext dbContext, IAuditLogger auditLogger)
{
    public async Task HandleAsync(ProviderEvent providerEvent, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<WebhookEvent>()
            .SingleOrDefaultAsync(e => e.ProviderEventId == providerEvent.ProviderEventId, cancellationToken);
        if (existing is not null)
        {
            // P3.08: same event delivered twice → processed once. Not an error — webhook
            // retries are expected and must be safe to no-op.
            return;
        }

        var purchase = await dbContext.Set<Purchase>()
            .SingleOrDefaultAsync(p => p.Id == providerEvent.PurchaseId, cancellationToken);
        if (purchase is null)
        {
            // Persist even an event for an unknown purchase — never silently drop provider
            // data — but there is nothing to transition.
            dbContext.Set<WebhookEvent>().Add(
                WebhookEvent.Create(providerEvent.ProviderEventId, providerEvent.Type.ToString(), null, providerEvent.PayloadJson, providerEvent.ProviderTimestampUtc));
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var webhookEvent = WebhookEvent.Create(
            providerEvent.ProviderEventId, providerEvent.Type.ToString(), purchase.Id, providerEvent.PayloadJson, providerEvent.ProviderTimestampUtc);
        dbContext.Set<WebhookEvent>().Add(webhookEvent);

        // P3.09: a later-timestamped event already processed for this purchase means this one
        // arrived out of order — record it, but never regress state from a stale event.
        var latestProcessedTimestamp = await dbContext.Set<WebhookEvent>()
            .Where(e => e.PurchaseId == purchase.Id && e.ProcessedAtUtc != null)
            .OrderByDescending(e => e.ProviderTimestampUtc)
            .Select(e => (DateTime?)e.ProviderTimestampUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;

        if (latestProcessedTimestamp is not null && providerEvent.ProviderTimestampUtc < latestProcessedTimestamp)
        {
            webhookEvent.MarkProcessed(utcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var businessOutcomeEntries = new List<AuditEntry>();
        await ApplyTransitionAsync(purchase, providerEvent, utcNow, businessOutcomeEntries, cancellationToken);

        webhookEvent.MarkProcessed(utcNow);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two concurrency races can land here:
            //  (a) the SAME event delivered twice racing past the pre-check above — both
            //      requests see "no existing row" before either commits. The database's unique
            //      index on WebhookEvent.ProviderEventId is the real guard; losing this race
            //      means the other request already fully processed this exact event, so this
            //      one is a safe no-op.
            //  (b) two DIFFERENT events (rarer — e.g. a provider re-minting a new event id per
            //      retry) racing to grant the same (UserId, ProgramId) entitlement. Here our own
            //      WebhookEvent row is still genuinely new data that must not be silently
            //      dropped — only the entitlement insert needs to be dropped and retried as a
            //      reactivate-or-no-op.
            var lostToDuplicateDelivery = await dbContext.Set<WebhookEvent>().AsNoTracking()
                .AnyAsync(e => e.ProviderEventId == providerEvent.ProviderEventId, cancellationToken);
            if (lostToDuplicateDelivery)
            {
                return;
            }

            var addedEntitlement = dbContext.ChangeTracker.Entries<ProgramEntitlement>()
                .SingleOrDefault(e => e.State == EntityState.Added);
            var raceWinner = addedEntitlement is not null
                ? await dbContext.Set<ProgramEntitlement>().AsNoTracking()
                    .SingleOrDefaultAsync(e => e.UserId == purchase.UserId && e.ProgramId == purchase.ProgramId, cancellationToken)
                : null;
            if (raceWinner is null)
            {
                throw;
            }

            // A concurrent delivery already granted the entitlement between our read and write.
            // Drop our own conflicting insert and retry so the WebhookEvent/Payment/Invoice rows
            // for THIS event are not lost.
            addedEntitlement!.State = EntityState.Detached;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await auditLogger.LogAsync(
            AuditEntry.Create(
                AuditActions.PaymentWebhookProcessed,
                entityType: "Purchase",
                entityId: purchase.Id.ToString(),
                metadata: new Dictionary<string, string> { ["eventType"] = providerEvent.Type.ToString(), ["webhookEventId"] = webhookEvent.Id.ToString() }),
            cancellationToken);

        // Distinct business-outcome audit entries (purchase.succeeded/refunded,
        // program_access.granted/revoked — docs/PROMPT.md §37) logged only after the same
        // transaction that produced them has actually committed, alongside the generic
        // payment.webhook_processed entry above.
        foreach (var entry in businessOutcomeEntries)
        {
            await auditLogger.LogAsync(entry, cancellationToken);
        }
    }

    private async Task ApplyTransitionAsync(
        Purchase purchase, ProviderEvent providerEvent, DateTime utcNow, List<AuditEntry> businessOutcomeEntries, CancellationToken cancellationToken)
    {
        switch (providerEvent.Type)
        {
            case ProviderEventType.PaymentSucceeded:
                await HandleSuccessfulPaymentAsync(purchase, providerEvent, utcNow, businessOutcomeEntries, cancellationToken);
                break;

            case ProviderEventType.PaymentFailed:
                HandleFailedPayment(purchase, providerEvent, utcNow);
                break;

            case ProviderEventType.PaymentRefunded:
                TryTransition(purchase, p => p.MarkRefunded());
                await RevokeEntitlementAsync(purchase, "refund", utcNow, businessOutcomeEntries, cancellationToken);
                businessOutcomeEntries.Add(
                    AuditEntry.Create(AuditActions.PurchaseRefunded, actorUserId: purchase.UserId, entityType: "Purchase", entityId: purchase.Id.ToString()));
                break;

            case ProviderEventType.PaymentChargedBack:
                TryTransition(purchase, p => p.MarkChargedBack());
                await RevokeEntitlementAsync(purchase, "chargeback", utcNow, businessOutcomeEntries, cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(providerEvent));
        }
    }

    /// <summary>Converts an invalid-transition <see cref="InvalidOperationException"/> into a
    /// clean business error instead of an unhandled 500.</summary>
    private static void TryTransition(Purchase purchase, Action<Purchase> transition)
    {
        try
        {
            transition(purchase);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleAppException("PURCHASE_STATUS_TRANSITION_INVALID", "errors.billing.invalidStatusTransition", ex.Message);
        }
    }

    private async Task HandleSuccessfulPaymentAsync(
        Purchase purchase, ProviderEvent providerEvent, DateTime utcNow, List<AuditEntry> businessOutcomeEntries, CancellationToken cancellationToken)
    {
        TryTransition(purchase, p => p.MarkSucceeded(utcNow));

        if (providerEvent.Amount is decimal amount && providerEvent.Currency is string currency)
        {
            dbContext.Set<Payment>().Add(Payment.Create(purchase.Id, amount, currency, PaymentStatus.Succeeded, providerEvent.ProviderEventId, utcNow));
            dbContext.Set<Invoice>().Add(Invoice.Create(purchase.Id, null, amount, currency, InvoiceStatus.Paid, utcNow));
        }

        await GrantOrReactivateEntitlementAsync(purchase, utcNow, businessOutcomeEntries, cancellationToken);

        businessOutcomeEntries.Add(
            AuditEntry.Create(AuditActions.PurchaseSucceeded, actorUserId: purchase.UserId, entityType: "Purchase", entityId: purchase.Id.ToString()));
    }

    private void HandleFailedPayment(Purchase purchase, ProviderEvent providerEvent, DateTime utcNow)
    {
        if (providerEvent.Amount is decimal amount && providerEvent.Currency is string currency)
        {
            dbContext.Set<Payment>().Add(Payment.Create(purchase.Id, amount, currency, PaymentStatus.Failed, providerEvent.ProviderEventId, utcNow));
        }

        // Only a Pending purchase transitions to Failed; a payment failing after the purchase
        // already Succeeded/Refunded/Chargeback/Failed is recorded for history only — there is
        // nothing to regress.
        if (purchase.Status == PurchaseStatus.Pending)
        {
            TryTransition(purchase, p => p.MarkFailed());
        }
    }

    /// <summary>Grants a brand-new <see cref="ProgramEntitlement"/> or reactivates an existing
    /// (previously revoked) one — never a duplicate row, per the unique (UserId, ProgramId)
    /// index. Does not save; tracked additions are flushed by <see cref="HandleAsync"/>'s single
    /// outer <c>SaveChangesAsync</c>, whose catch block handles a concurrent delivery racing this
    /// exact insert (P3.38/39's concurrency guard) — see that method's comment.</summary>
    private async Task GrantOrReactivateEntitlementAsync(
        Purchase purchase, DateTime utcNow, List<AuditEntry> businessOutcomeEntries, CancellationToken cancellationToken)
    {
        var entitlement = await dbContext.Set<ProgramEntitlement>()
            .SingleOrDefaultAsync(e => e.UserId == purchase.UserId && e.ProgramId == purchase.ProgramId, cancellationToken);

        if (entitlement is null)
        {
            entitlement = ProgramEntitlement.Grant(purchase.UserId, purchase.ProgramId, purchase.Id, utcNow);
            dbContext.Set<ProgramEntitlement>().Add(entitlement);
            businessOutcomeEntries.Add(
                AuditEntry.Create(AuditActions.ProgramAccessGranted, actorUserId: purchase.UserId, entityType: "ProgramEntitlement", entityId: purchase.ProgramId.ToString()));
        }
        else if (entitlement.Status != ProgramEntitlementStatus.Active)
        {
            entitlement.Reactivate(purchase.Id, utcNow);
            businessOutcomeEntries.Add(
                AuditEntry.Create(AuditActions.ProgramAccessGranted, actorUserId: purchase.UserId, entityType: "ProgramEntitlement", entityId: purchase.ProgramId.ToString()));
        }

        // else: already Active — idempotent no-op (duplicate delivery already processed).
    }

    private async Task RevokeEntitlementAsync(
        Purchase purchase, string reason, DateTime utcNow, List<AuditEntry> businessOutcomeEntries, CancellationToken cancellationToken)
    {
        var entitlement = await dbContext.Set<ProgramEntitlement>()
            .SingleOrDefaultAsync(e => e.UserId == purchase.UserId && e.ProgramId == purchase.ProgramId, cancellationToken);
        if (entitlement is null || entitlement.Status != ProgramEntitlementStatus.Active)
        {
            return;
        }

        entitlement.Revoke(reason, utcNow);
        businessOutcomeEntries.Add(
            AuditEntry.Create(AuditActions.ProgramAccessRevoked, actorUserId: purchase.UserId, entityType: "ProgramEntitlement", entityId: purchase.ProgramId.ToString()));
    }
}

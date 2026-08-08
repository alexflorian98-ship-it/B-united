using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Application.Configuration;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Billing.Application.UseCases;

/// <summary>The single choke point every provider event (fake, for V1 — ADR-010) passes
/// through, whether it arrived via the demo webhook HTTP endpoint or was generated in-process by
/// checkout/demo-action handlers. Owns idempotency (P3.08), out-of-order safety (P3.09), the
/// state-machine transitions (P3.10, docs/ARCHITECTURE.md §8), the resulting
/// <see cref="Entitlement"/> update (P3.11), and the audit trail (P3.12) — every caller gets all
/// of that for free rather than re-implementing it.</summary>
public sealed class ProcessProviderEventHandler(DbContext dbContext, IAuditLogger auditLogger, IOptions<BillingOptions> billingOptions)
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

        var subscription = await dbContext.Set<Subscription>()
            .SingleOrDefaultAsync(s => s.Id == providerEvent.SubscriptionId, cancellationToken);
        if (subscription is null)
        {
            // Persist even an event for an unknown subscription — never silently drop provider
            // data — but there is nothing to transition.
            dbContext.Set<WebhookEvent>().Add(
                WebhookEvent.Create(providerEvent.ProviderEventId, providerEvent.Type.ToString(), null, providerEvent.PayloadJson, providerEvent.ProviderTimestampUtc));
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var webhookEvent = WebhookEvent.Create(
            providerEvent.ProviderEventId, providerEvent.Type.ToString(), subscription.Id, providerEvent.PayloadJson, providerEvent.ProviderTimestampUtc);
        dbContext.Set<WebhookEvent>().Add(webhookEvent);

        // P3.09: a later-timestamped event already processed for this subscription means this
        // one arrived out of order — record it, but never regress state from a stale event.
        var latestProcessedTimestamp = await dbContext.Set<WebhookEvent>()
            .Where(e => e.SubscriptionId == subscription.Id && e.ProcessedAtUtc != null)
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

        await ApplyTransitionAsync(subscription, providerEvent, utcNow, cancellationToken);

        webhookEvent.MarkProcessed(utcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(
                AuditActions.PaymentWebhookProcessed,
                entityType: "Subscription",
                entityId: subscription.Id.ToString(),
                metadata: new Dictionary<string, string> { ["eventType"] = providerEvent.Type.ToString(), ["webhookEventId"] = webhookEvent.Id.ToString() }),
            cancellationToken);
    }

    private async Task ApplyTransitionAsync(Subscription subscription, ProviderEvent providerEvent, DateTime utcNow, CancellationToken cancellationToken)
    {
        var currentPeriod = await dbContext.Set<SubscriptionPeriod>()
            .Where(p => p.SubscriptionId == subscription.Id)
            .OrderByDescending(p => p.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);

        switch (providerEvent.Type)
        {
            case ProviderEventType.SubscriptionActivated:
            case ProviderEventType.PaymentSucceeded:
            case ProviderEventType.SubscriptionRenewed:
                await HandleSuccessfulPaymentAsync(subscription, currentPeriod, providerEvent, utcNow, cancellationToken);
                break;

            case ProviderEventType.PaymentFailed:
                await HandleFailedPaymentAsync(subscription, currentPeriod, providerEvent, utcNow, cancellationToken);
                break;

            case ProviderEventType.SubscriptionCanceled:
                TryTransition(subscription, s => s.Cancel(utcNow));
                await UpsertEntitlementAsync(subscription.UserId, currentPeriod?.PeriodEnd ?? utcNow, cancellationToken);
                break;

            case ProviderEventType.SubscriptionExpired:
                TryTransition(subscription, s => s.Expire());
                await RevokeEntitlementAsync(subscription.UserId, cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(providerEvent));
        }
    }

    /// <summary>Converts an invalid-transition <see cref="InvalidOperationException"/> (e.g.
    /// attempting to "renew" a Canceled-but-not-yet-Expired subscription — the state diagram,
    /// docs/ARCHITECTURE.md §8, has no direct Canceled -> Active edge) into a clean business
    /// error instead of an unhandled 500.</summary>
    private static void TryTransition(Subscription subscription, Action<Subscription> transition)
    {
        try
        {
            transition(subscription);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleAppException("SUBSCRIPTION_STATUS_TRANSITION_INVALID", "errors.billing.invalidStatusTransition", ex.Message);
        }
    }

    private async Task HandleSuccessfulPaymentAsync(Subscription subscription, SubscriptionPeriod? currentPeriod, ProviderEvent providerEvent, DateTime utcNow, CancellationToken cancellationToken)
    {
        TryTransition(subscription, s => s.Activate());

        var planPrice = await dbContext.Set<Domain.Entities.PlanPrice>().SingleAsync(p => p.Id == subscription.PlanPriceId, cancellationToken);
        var periodStart = currentPeriod is not null && currentPeriod.PeriodEnd > utcNow ? currentPeriod.PeriodEnd : utcNow;
        var periodEnd = planPrice.Interval == BillingInterval.Yearly ? periodStart.AddYears(1) : periodStart.AddMonths(1);

        dbContext.Set<SubscriptionPeriod>().Add(SubscriptionPeriod.Create(subscription.Id, periodStart, periodEnd, periodEnd));

        if (providerEvent.Amount is decimal amount && providerEvent.Currency is string currency)
        {
            dbContext.Set<Payment>().Add(Payment.Create(subscription.Id, amount, currency, PaymentStatus.Succeeded, providerEvent.ProviderEventId, utcNow));
            dbContext.Set<Invoice>().Add(Invoice.Create(subscription.Id, null, amount, currency, InvoiceStatus.Paid, utcNow));
        }

        // Active access has no hard cutoff — the next webhook (renewal or failure) is what
        // will move it, matching how a real provider never lets a period silently lapse
        // without eventually sending a corresponding event.
        await UpsertEntitlementAsync(subscription.UserId, null, cancellationToken);
    }

    private async Task HandleFailedPaymentAsync(Subscription subscription, SubscriptionPeriod? currentPeriod, ProviderEvent providerEvent, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (providerEvent.Amount is decimal amount && providerEvent.Currency is string currency)
        {
            dbContext.Set<Payment>().Add(Payment.Create(subscription.Id, amount, currency, PaymentStatus.Failed, providerEvent.ProviderEventId, utcNow));
        }

        if (subscription.Status == SubscriptionStatus.Trialing)
        {
            // docs/ARCHITECTURE.md §8: Trialing -> Expired: trial ends unpaid.
            subscription.Expire();
            await RevokeEntitlementAsync(subscription.UserId, cancellationToken);
            return;
        }

        if (subscription.Status == SubscriptionStatus.Active)
        {
            subscription.MarkPastDue();
            var graceCutoff = (currentPeriod?.PaidPeriodEnd ?? utcNow).AddDays(billingOptions.Value.GracePeriodDays);
            await UpsertEntitlementAsync(subscription.UserId, graceCutoff, cancellationToken);
        }

        // Already PastDue/Canceled/Expired: the failed payment is recorded for history, but the
        // subscription's status/entitlement don't change further — there is nothing to regress.
    }

    private async Task UpsertEntitlementAsync(Guid userId, DateTime? validUntilUtc, CancellationToken cancellationToken)
    {
        var entitlement = await dbContext.Set<Entitlement>()
            .SingleOrDefaultAsync(e => e.UserId == userId && e.Type == Entitlement.PlatformAccessType, cancellationToken);

        if (entitlement is null)
        {
            var subscription = await dbContext.Set<Subscription>().SingleAsync(s => s.UserId == userId, cancellationToken);
            dbContext.Set<Entitlement>().Add(Entitlement.Grant(userId, Entitlement.PlatformAccessType, DateTime.UtcNow, validUntilUtc, subscription.Id));
        }
        else
        {
            entitlement.Extend(validUntilUtc);
        }
    }

    private async Task RevokeEntitlementAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entitlement = await dbContext.Set<Entitlement>()
            .SingleOrDefaultAsync(e => e.UserId == userId && e.Type == Entitlement.PlatformAccessType, cancellationToken);
        entitlement?.Revoke();
    }
}

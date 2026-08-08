using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

/// <summary>§54 subscription detail view, including the webhook timeline. Raw payload visibility
/// (P3.22) is decided by the caller — the Api layer only passes <paramref name="includeRawPayload"/>
/// as <see langword="true"/> for callers holding the dedicated technical-administrator
/// permission; every other caller gets the timeline with <c>PayloadJson</c> always
/// <see langword="null"/>.</summary>
public sealed class GetSubscriptionDetailHandler(DbContext dbContext, IUserLookup userLookup)
{
    public async Task<SubscriptionDetailDto> HandleAsync(Guid subscriptionId, bool includeRawPayload, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Set<Subscription>().AsNoTracking().SingleOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken)
            ?? throw new NotFoundAppException("The specified subscription does not exist.");

        var plan = await dbContext.Set<Plan>().AsNoTracking().SingleAsync(p => p.Id == subscription.PlanId, cancellationToken);

        var currentPeriod = await dbContext.Set<SubscriptionPeriod>().AsNoTracking()
            .Where(p => p.SubscriptionId == subscriptionId)
            .OrderByDescending(p => p.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);

        var payments = await dbContext.Set<Payment>().AsNoTracking()
            .Where(p => p.SubscriptionId == subscriptionId)
            .OrderByDescending(p => p.OccurredAtUtc)
            .Select(p => new PaymentDto(p.Id, p.Amount, p.Currency, p.Status.ToString(), p.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.Set<Invoice>().AsNoTracking()
            .Where(i => i.SubscriptionId == subscriptionId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .Select(i => new InvoiceDto(i.Id, i.Amount, i.Currency, i.Status.ToString(), i.IssuedAtUtc))
            .ToListAsync(cancellationToken);

        var webhookEvents = await dbContext.Set<WebhookEvent>().AsNoTracking()
            .Where(e => e.SubscriptionId == subscriptionId)
            .OrderByDescending(e => e.ProviderTimestampUtc)
            .ToListAsync(cancellationToken);

        var entitlement = await dbContext.Set<Entitlement>().AsNoTracking()
            .SingleOrDefaultAsync(e => e.UserId == subscription.UserId && e.Type == Entitlement.PlatformAccessType, cancellationToken);

        var user = await userLookup.GetByIdAsync(subscription.UserId, cancellationToken);

        return new SubscriptionDetailDto(
            subscription.Id,
            subscription.UserId,
            user?.Email,
            plan.Name,
            subscription.Status.ToString(),
            currentPeriod is null ? null : new SubscriptionPeriodDto(currentPeriod.PeriodStart, currentPeriod.PeriodEnd, currentPeriod.PaidPeriodEnd),
            entitlement?.ValidUntilUtc,
            payments,
            invoices,
            webhookEvents.Select(e => new WebhookEventSummaryDto(e.Id, e.EventType, e.ProviderTimestampUtc, e.ProcessedAtUtc, includeRawPayload ? e.PayloadJson : null)).ToList());
    }
}

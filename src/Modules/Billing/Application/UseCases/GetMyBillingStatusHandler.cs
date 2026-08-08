using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

public sealed class GetMyBillingStatusHandler(DbContext dbContext)
{
    public async Task<MyBillingStatusDto> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Set<Subscription>().AsNoTracking().SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        var entitlement = await dbContext.Set<Entitlement>().AsNoTracking()
            .SingleOrDefaultAsync(e => e.UserId == userId && e.Type == Entitlement.PlatformAccessType, cancellationToken);
        var utcNow = DateTime.UtcNow;
        var hasActiveAccess = entitlement is not null && entitlement.IsActiveAt(utcNow);

        if (subscription is null)
        {
            return new MyBillingStatusDto(null, null, null, null, hasActiveAccess, entitlement?.ValidUntilUtc, [], []);
        }

        var plan = await dbContext.Set<Plan>().AsNoTracking().SingleAsync(p => p.Id == subscription.PlanId, cancellationToken);

        var currentPeriod = await dbContext.Set<SubscriptionPeriod>().AsNoTracking()
            .Where(p => p.SubscriptionId == subscription.Id)
            .OrderByDescending(p => p.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);

        var payments = await dbContext.Set<Payment>().AsNoTracking()
            .Where(p => p.SubscriptionId == subscription.Id)
            .OrderByDescending(p => p.OccurredAtUtc)
            .Select(p => new PaymentDto(p.Id, p.Amount, p.Currency, p.Status.ToString(), p.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.Set<Invoice>().AsNoTracking()
            .Where(i => i.SubscriptionId == subscription.Id)
            .OrderByDescending(i => i.IssuedAtUtc)
            .Select(i => new InvoiceDto(i.Id, i.Amount, i.Currency, i.Status.ToString(), i.IssuedAtUtc))
            .ToListAsync(cancellationToken);

        return new MyBillingStatusDto(
            subscription.Id,
            plan.Name,
            subscription.Status.ToString(),
            currentPeriod is null ? null : new SubscriptionPeriodDto(currentPeriod.PeriodStart, currentPeriod.PeriodEnd, currentPeriod.PaidPeriodEnd),
            hasActiveAccess,
            entitlement?.ValidUntilUtc,
            payments,
            invoices);
    }
}

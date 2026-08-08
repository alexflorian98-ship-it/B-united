using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

/// <summary>§54 subscriber table. Resolves client emails via <see cref="IUserLookup"/> — a
/// read-only cross-module admin projection (CLAUDE.md), never a reference to Identity's Domain.</summary>
public sealed class ListSubscribersHandler(DbContext dbContext, IUserLookup userLookup)
{
    public async Task<SubscriberListResult> HandleAsync(ListSubscribersQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.Set<Subscription>().AsNoTracking().OrderByDescending(s => s.CreatedAt);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var page = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var subscriptionIds = page.Select(s => s.Id).ToList();

        var periods = await dbContext.Set<SubscriptionPeriod>().AsNoTracking()
            .Where(p => subscriptionIds.Contains(p.SubscriptionId))
            .ToListAsync(cancellationToken);

        var userIds = page.Select(s => s.UserId).Distinct().ToList();
        var users = await userLookup.GetByIdsAsync(userIds, cancellationToken);

        var entitlements = await dbContext.Set<Entitlement>().AsNoTracking()
            .Where(e => userIds.Contains(e.UserId) && e.Type == Entitlement.PlatformAccessType)
            .ToListAsync(cancellationToken);

        var recentFailedPayments = await dbContext.Set<Payment>().AsNoTracking()
            .Where(p => subscriptionIds.Contains(p.SubscriptionId))
            .GroupBy(p => p.SubscriptionId)
            .Select(g => new { SubscriptionId = g.Key, LatestStatus = g.OrderByDescending(p => p.OccurredAtUtc).First().Status })
            .ToListAsync(cancellationToken);

        var items = page.Select(subscription =>
        {
            var currentPeriod = periods.Where(p => p.SubscriptionId == subscription.Id).OrderByDescending(p => p.PeriodEnd).FirstOrDefault();
            var entitlement = entitlements.SingleOrDefault(e => e.UserId == subscription.UserId);
            var latestPayment = recentFailedPayments.SingleOrDefault(p => p.SubscriptionId == subscription.Id);

            return new SubscriberSummaryDto(
                subscription.UserId,
                users.TryGetValue(subscription.UserId, out var user) ? user.Email : null,
                subscription.Id,
                subscription.Status.ToString(),
                currentPeriod?.PeriodEnd,
                entitlement?.ValidUntilUtc,
                latestPayment?.LatestStatus.ToString() ?? "None",
                subscription.CreatedAt);
        }).ToList();

        return new SubscriberListResult(items, totalCount);
    }
}

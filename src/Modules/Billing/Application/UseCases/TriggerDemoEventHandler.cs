using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

/// <summary>P3.18: local demo-only subscription controls (renew/fail payment/cancel/expire),
/// driving the exact same fake-event pipeline a real provider's test-mode webhook would —
/// see ADR-010. Never available for anything other than the caller's own subscription.</summary>
public sealed class TriggerDemoEventHandler(DbContext dbContext, IPaymentProvider paymentProvider, ProcessProviderEventHandler processProviderEventHandler)
{
    public async Task HandleAsync(TriggerDemoEventCommand command, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Set<Subscription>().SingleOrDefaultAsync(s => s.UserId == command.UserId, cancellationToken)
            ?? throw new NotFoundAppException("No subscription exists for this user.");

        var planPrice = await dbContext.Set<PlanPrice>().SingleAsync(p => p.Id == subscription.PlanPriceId, cancellationToken);

        var eventType = command.Action switch
        {
            DemoAction.Renew => ProviderEventType.SubscriptionRenewed,
            DemoAction.FailPayment => ProviderEventType.PaymentFailed,
            DemoAction.Cancel => ProviderEventType.SubscriptionCanceled,
            DemoAction.Expire => ProviderEventType.SubscriptionExpired,
            _ => throw new ArgumentOutOfRangeException(nameof(command)),
        };

        var utcNow = DateTime.UtcNow;
        var providerEvent = paymentProvider.CreateDemoEvent(subscription.Id, eventType, planPrice.Amount, planPrice.Currency, utcNow);
        await processProviderEventHandler.HandleAsync(providerEvent, cancellationToken);
    }
}

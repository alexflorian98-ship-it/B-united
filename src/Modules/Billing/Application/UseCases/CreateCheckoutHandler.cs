using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

/// <summary>docs/PROMPT.md §17: "Checkout Session → Stripe → Webhook → Billing module →
/// Subscription → Entitlement." Access is granted only by <see cref="ProcessProviderEventHandler"/>
/// processing the resulting provider event — never directly by this handler — so the same rule
/// ("checkout-success redirect is informational only") holds even though the fake provider
/// resolves synchronously (ADR-010).</summary>
public sealed class CreateCheckoutHandler(DbContext dbContext, IPaymentProvider paymentProvider, ProcessProviderEventHandler processProviderEventHandler, IAuditLogger auditLogger)
{
    public async Task<CheckoutResultDto> HandleAsync(CreateCheckoutCommand command, CancellationToken cancellationToken)
    {
        var planPrice = await dbContext.Set<PlanPrice>().SingleOrDefaultAsync(p => p.Id == command.PlanPriceId && p.IsActive, cancellationToken)
            ?? throw new NotFoundAppException("The specified plan price does not exist or is not active.");

        var subscription = await dbContext.Set<Subscription>().SingleOrDefaultAsync(s => s.UserId == command.UserId, cancellationToken);

        if (subscription is not null && subscription.Status is SubscriptionStatus.Trialing or SubscriptionStatus.Active or SubscriptionStatus.PastDue)
        {
            throw new BusinessRuleAppException(
                "SUBSCRIPTION_ALREADY_EXISTS",
                "errors.billing.subscriptionAlreadyExists",
                "This user already has an active or pending subscription.");
        }

        var isNewSubscription = subscription is null;
        if (subscription is null)
        {
            subscription = Subscription.StartTrial(command.UserId, planPrice.PlanId, planPrice.Id);
            dbContext.Set<Subscription>().Add(subscription);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (isNewSubscription)
        {
            await auditLogger.LogAsync(
                AuditEntry.Create(AuditActions.SubscriptionCreated, actorUserId: command.UserId, entityType: "Subscription", entityId: subscription.Id.ToString()),
                cancellationToken);
        }

        await paymentProvider.EnsureCustomerAsync(command.UserId, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var providerEvent = paymentProvider.SimulateCheckout(subscription.Id, planPrice.Amount, planPrice.Currency, command.Outcome, utcNow);

        if (providerEvent is null)
        {
            return new CheckoutResultDto(subscription.Id, Activated: false, DeclineReason: command.Outcome.ToString());
        }

        await processProviderEventHandler.HandleAsync(providerEvent, cancellationToken);

        var refreshed = await dbContext.Set<Subscription>().AsNoTracking().SingleAsync(s => s.Id == subscription.Id, cancellationToken);
        return new CheckoutResultDto(subscription.Id, Activated: refreshed.Status == SubscriptionStatus.Active, DeclineReason: refreshed.Status == SubscriptionStatus.Active ? null : "declined");
    }
}

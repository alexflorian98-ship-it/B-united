using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Contracts;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Content.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

/// <summary>docs/PROMPT.md §17: "Checkout Session → Stripe → Webhook → Billing module →
/// Purchase → ProgramEntitlement." Access is granted only by
/// <see cref="ProcessProviderEventHandler"/> processing the resulting provider event — never
/// directly by this handler — so the same rule ("checkout-success redirect is informational
/// only") holds even though the fake provider resolves synchronously (ADR-010). The client sends
/// only <see cref="CreateProgramPurchaseCommand.ProgramId"/>: amount/currency are always resolved
/// server-side from the active offer, never trusted from the browser.</summary>
public sealed class CreateProgramPurchaseHandler(
    DbContext dbContext,
    IPaymentProvider paymentProvider,
    IProgramAccessContext programAccessContext,
    IProgramOfferLookup programOfferLookup,
    IProgramLookup programLookup,
    ProcessProviderEventHandler processProviderEventHandler,
    IAuditLogger auditLogger)
{
    public async Task<CheckoutResultDto> HandleAsync(CreateProgramPurchaseCommand command, CancellationToken cancellationToken)
    {
        var alreadyOwned = await programAccessContext.HasProgramAccessAsync(command.UserId, command.ProgramId, cancellationToken);
        if (alreadyOwned)
        {
            throw new BusinessRuleAppException(
                ProgramAccessErrorCodes.ProgramAlreadyOwned,
                "errors.billing.programAlreadyOwned",
                "This program has already been purchased.");
        }

        var purchase = await dbContext.Set<Purchase>()
            .SingleOrDefaultAsync(p => p.UserId == command.UserId && p.ProgramId == command.ProgramId && p.Status == PurchaseStatus.Pending, cancellationToken);

        var isNewPurchase = purchase is null;
        if (purchase is null)
        {
            var offer = await programOfferLookup.GetActiveOfferAsync(command.ProgramId, cancellationToken)
                ?? throw new NotFoundAppException("No active offer exists for the specified program.");

            var program = await programLookup.GetProgramAsync(command.ProgramId, cancellationToken);

            purchase = Purchase.Create(command.UserId, command.ProgramId, offer.ProgramOfferId, offer.ProgramPriceId, offer.Amount, offer.Currency, program?.Title);
            dbContext.Set<Purchase>().Add(purchase);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (isNewPurchase)
        {
            await auditLogger.LogAsync(
                AuditEntry.Create(AuditActions.PurchaseCreated, actorUserId: command.UserId, entityType: "Purchase", entityId: purchase.Id.ToString()),
                cancellationToken);
        }

        await paymentProvider.EnsureCustomerAsync(command.UserId, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var providerEvent = paymentProvider.SimulateCheckout(purchase.Id, purchase.Amount, purchase.Currency, command.Outcome, utcNow);

        if (providerEvent is null)
        {
            // ProviderError/Timeout: no webhook is ever produced — the purchase stays Pending
            // until a later demo action or real retry resolves it.
            return new CheckoutResultDto(purchase.Id, purchase.ProgramId, purchase.Status.ToString());
        }

        await processProviderEventHandler.HandleAsync(providerEvent, cancellationToken);

        var refreshed = await dbContext.Set<Purchase>().AsNoTracking().SingleAsync(p => p.Id == purchase.Id, cancellationToken);
        return new CheckoutResultDto(refreshed.Id, refreshed.ProgramId, refreshed.Status.ToString());
    }
}

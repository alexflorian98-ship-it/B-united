using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

/// <summary>§54 purchase detail view, including the webhook timeline. Raw payload visibility
/// (P3.22) is decided by the caller — the Api layer only passes <paramref name="includeRawPayload"/>
/// as <see langword="true"/> for callers holding the dedicated technical-administrator
/// permission; every other caller gets the timeline with <c>PayloadJson</c> always
/// <see langword="null"/>.</summary>
public sealed class GetPurchaseDetailHandler(DbContext dbContext, IUserLookup userLookup)
{
    public async Task<PurchaseDetailDto> HandleAsync(Guid purchaseId, bool includeRawPayload, CancellationToken cancellationToken)
    {
        var purchase = await dbContext.Set<Purchase>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == purchaseId, cancellationToken)
            ?? throw new NotFoundAppException("The specified purchase does not exist.");

        var payments = await dbContext.Set<Payment>().AsNoTracking()
            .Where(p => p.PurchaseId == purchaseId)
            .OrderByDescending(p => p.OccurredAtUtc)
            .Select(p => new PaymentDto(p.Id, p.Amount, p.Currency, p.Status.ToString(), p.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.Set<Invoice>().AsNoTracking()
            .Where(i => i.PurchaseId == purchaseId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .Select(i => new InvoiceDto(i.Id, i.Amount, i.Currency, i.Status.ToString(), i.IssuedAtUtc))
            .ToListAsync(cancellationToken);

        var webhookEvents = await dbContext.Set<WebhookEvent>().AsNoTracking()
            .Where(e => e.PurchaseId == purchaseId)
            .OrderByDescending(e => e.ProviderTimestampUtc)
            .ToListAsync(cancellationToken);

        var entitlement = await dbContext.Set<ProgramEntitlement>().AsNoTracking()
            .SingleOrDefaultAsync(e => e.UserId == purchase.UserId && e.ProgramId == purchase.ProgramId, cancellationToken);

        var user = await userLookup.GetByIdAsync(purchase.UserId, cancellationToken);

        return new PurchaseDetailDto(
            purchase.Id,
            purchase.UserId,
            user?.Email,
            purchase.ProgramId,
            purchase.ProgramTitleSnapshot,
            purchase.Amount,
            purchase.Currency,
            purchase.Status.ToString(),
            purchase.CreatedAt,
            purchase.CompletedAtUtc,
            entitlement?.Status.ToString(),
            payments,
            invoices,
            webhookEvents.Select(e => new WebhookEventSummaryDto(e.Id, e.EventType, e.ProviderTimestampUtc, e.ProcessedAtUtc, includeRawPayload ? e.PayloadJson : null)).ToList());
    }
}

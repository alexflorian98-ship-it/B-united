using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

/// <summary>P3.35/36 — always inserts a new <see cref="ProgramPrice"/> row, never mutates an
/// existing one: a <c>Purchase</c> snapshots its amount/currency at checkout time, so rewriting
/// price history would silently corrupt that snapshot's provenance. Checkout (see
/// <c>CreateProgramPurchaseHandler</c>/<c>ProgramOfferLookup</c>) already resolves "current
/// price" as the latest <see cref="ProgramPrice"/> by <see cref="ProgramPrice.CreatedAtUtc"/> for
/// the active offer, so a newly inserted row here is picked up automatically.</summary>
public sealed class UpdateProgramOfferPriceHandler(DbContext dbContext, IAuditLogger auditLogger)
{
    public async Task<Guid> HandleAsync(UpdateProgramOfferPriceCommand command, CancellationToken cancellationToken)
    {
        var offerExists = await dbContext.Set<ProgramOffer>().AnyAsync(o => o.Id == command.ProgramOfferId, cancellationToken);
        if (!offerExists)
        {
            throw new NotFoundAppException("The specified program offer does not exist.");
        }

        var price = ProgramPrice.Create(command.ProgramOfferId, command.Amount, command.Currency, DateTime.UtcNow);
        dbContext.Set<ProgramPrice>().Add(price);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(
                AuditActions.ProgramOfferPriceChanged,
                actorUserId: command.ActorId,
                entityType: "ProgramOffer",
                entityId: command.ProgramOfferId.ToString()),
            cancellationToken);

        return price.Id;
    }
}

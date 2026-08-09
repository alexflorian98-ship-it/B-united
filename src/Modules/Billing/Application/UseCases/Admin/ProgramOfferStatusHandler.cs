using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

/// <summary>Mirrors Content's <c>ProgramStatusHandler</c> shape: both transitions are a one-line
/// domain call wrapped in the same load/mutate/save shape. Concurrency is guarded by
/// <see cref="ProgramOffer"/>'s Postgres <c>xmin</c> row-version (configured in
/// <c>ProgramOfferConfiguration</c>) — <see cref="DbUpdateConcurrencyException"/> from a
/// concurrent conflicting write is translated to a stable business-rule error instead of
/// bubbling up as an opaque 500.</summary>
public sealed class ProgramOfferStatusHandler(DbContext dbContext, IAuditLogger auditLogger)
{
    public Task ActivateAsync(Guid programOfferId, Guid actorId, CancellationToken cancellationToken) =>
        TransitionAsync(programOfferId, actorId, AuditActions.ProgramOfferActivated, async offer =>
        {
            var hasPrice = await dbContext.Set<ProgramPrice>().AnyAsync(p => p.ProgramOfferId == offer.Id, cancellationToken);
            if (!hasPrice)
            {
                throw new BusinessRuleAppException(
                    "PROGRAM_OFFER_NO_PRICE",
                    "errors.billing.programOfferHasNoPrice",
                    "A program offer must have a price before it can be activated.");
            }

            var conflictingActiveOfferExists = await dbContext.Set<ProgramOffer>()
                .AnyAsync(o => o.Id != offer.Id && o.ProgramId == offer.ProgramId && o.Status == ProgramOfferStatus.Active, cancellationToken);
            if (conflictingActiveOfferExists)
            {
                throw new BusinessRuleAppException(
                    "PROGRAM_OFFER_ALREADY_ACTIVE",
                    "errors.billing.programOfferAlreadyActive",
                    "An active offer already exists for this program.");
            }

            offer.Activate();
        }, cancellationToken);

    public Task DeactivateAsync(Guid programOfferId, Guid actorId, CancellationToken cancellationToken) =>
        TransitionAsync(programOfferId, actorId, AuditActions.ProgramOfferDeactivated, offer =>
        {
            offer.Deactivate();
            return Task.CompletedTask;
        }, cancellationToken);

    private async Task TransitionAsync(
        Guid programOfferId,
        Guid actorId,
        string auditAction,
        Func<ProgramOffer, Task> transition,
        CancellationToken cancellationToken)
    {
        var offer = await dbContext.Set<ProgramOffer>().SingleOrDefaultAsync(o => o.Id == programOfferId, cancellationToken)
            ?? throw new NotFoundAppException("The specified program offer does not exist.");

        await transition(offer);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleAppException(
                "PROGRAM_OFFER_CONCURRENCY_CONFLICT",
                "errors.billing.programOfferConcurrencyConflict",
                "The program offer was modified by another request. Reload and try again.");
        }

        await auditLogger.LogAsync(
            AuditEntry.Create(auditAction, actorUserId: actorId, entityType: "ProgramOffer", entityId: programOfferId.ToString()),
            cancellationToken);
    }
}

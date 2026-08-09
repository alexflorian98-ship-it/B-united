using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Content.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

/// <summary>P3.35 — admin creation of a program offer. <see cref="ProgramId"/> is validated
/// against Content via <see cref="IProgramLookup"/> (a Contracts-only cross-module read, never
/// Content's Domain/Infrastructure — CLAUDE.md) rather than a database foreign key, since Billing
/// must never reference Content's schema directly.</summary>
public sealed class CreateProgramOfferHandler(DbContext dbContext, IProgramLookup programLookup, IAuditLogger auditLogger)
{
    public async Task<CreateProgramOfferResultDto> HandleAsync(CreateProgramOfferCommand command, CancellationToken cancellationToken)
    {
        var program = await programLookup.GetProgramAsync(command.ProgramId, cancellationToken)
            ?? throw new NotFoundAppException("The specified program does not exist.");

        if (program.Status != ProgramLookupStatus.Published)
        {
            throw new BusinessRuleAppException(
                "PROGRAM_OFFER_PROGRAM_NOT_PUBLISHED",
                "errors.billing.programNotPublished",
                "An offer can only be created for a published program.");
        }

        var activeOfferExists = await dbContext.Set<ProgramOffer>()
            .AnyAsync(o => o.ProgramId == command.ProgramId && o.Status == ProgramOfferStatus.Active, cancellationToken);
        if (activeOfferExists)
        {
            throw new BusinessRuleAppException(
                "PROGRAM_OFFER_ALREADY_ACTIVE",
                "errors.billing.programOfferAlreadyActive",
                "An active offer already exists for this program.");
        }

        var utcNow = DateTime.UtcNow;
        var offer = ProgramOffer.Create(command.ProgramId);
        var price = ProgramPrice.Create(offer.Id, command.Amount, command.Currency, utcNow);

        if (command.ActivateImmediately)
        {
            offer.Activate();
        }

        dbContext.Set<ProgramOffer>().Add(offer);
        dbContext.Set<ProgramPrice>().Add(price);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(
                AuditActions.ProgramOfferCreated,
                actorUserId: command.ActorId,
                entityType: "ProgramOffer",
                entityId: offer.Id.ToString(),
                metadata: new Dictionary<string, string>
                {
                    ["programId"] = command.ProgramId.ToString(),
                    ["activatedImmediately"] = command.ActivateImmediately.ToString(),
                }),
            cancellationToken);

        return new CreateProgramOfferResultDto(offer.Id, price.Id);
    }
}

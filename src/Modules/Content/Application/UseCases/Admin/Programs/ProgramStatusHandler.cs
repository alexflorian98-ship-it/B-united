using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Chat.Contracts;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

/// <summary>All three status-transition actions in one handler — each is a one-line domain
/// call wrapped in the same load/catch/save shape, not enough distinct behavior to warrant
/// three separate classes.</summary>
public sealed class ProgramStatusHandler(DbContext dbContext, IAuditLogger auditLogger, IProgramChatRoomProvisioner chatRoomProvisioner)
{
    public async Task PublishAsync(Guid programId, Guid actorId, CancellationToken cancellationToken)
    {
        await TransitionAsync(programId, actorId, p => p.Publish(actorId), AuditActions.ContentPublished, cancellationToken);

        // Every published program gets exactly one chat room, named after the program itself, with
        // no separate manual admin step (product decision, 2026-08-18 — a program's community
        // space should never require an admin to remember to create it). Access to the room is
        // still gated dynamically by real program entitlement, same as everything else — this only
        // guarantees the room itself exists.
        var defaultLanguage = await dbContext.Set<Program>()
            .Where(p => p.Id == programId)
            .Select(p => p.DefaultLanguage)
            .SingleAsync(cancellationToken);

        var title = await dbContext.Set<ProgramTranslation>()
            .Where(t => t.ProgramId == programId && t.Language == defaultLanguage)
            .Select(t => t.Title)
            .SingleAsync(cancellationToken);

        await chatRoomProvisioner.EnsureRoomForProgramAsync(programId, title, actorId, cancellationToken);
    }

    public Task UnpublishAsync(Guid programId, Guid actorId, CancellationToken cancellationToken) =>
        TransitionAsync(programId, actorId, p => p.Unpublish(actorId), auditAction: null, cancellationToken);

    public Task ArchiveAsync(Guid programId, Guid actorId, CancellationToken cancellationToken) =>
        TransitionAsync(programId, actorId, p => p.Archive(actorId), auditAction: null, cancellationToken);

    private async Task TransitionAsync(
        Guid programId, Guid actorId, Action<Program> transition, string? auditAction, CancellationToken cancellationToken)
    {
        var program = await dbContext.Set<Program>().SingleOrDefaultAsync(p => p.Id == programId, cancellationToken)
            ?? throw new NotFoundAppException("The specified program does not exist.");

        try
        {
            transition(program);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleAppException("PROGRAM_STATUS_TRANSITION_INVALID", "errors.program.invalidStatusTransition", ex.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (auditAction is not null)
        {
            await auditLogger.LogAsync(
                AuditEntry.Create(auditAction, actorUserId: actorId, entityType: "Program", entityId: programId.ToString()),
                cancellationToken);
        }
    }
}

using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

/// <summary>All three status-transition actions in one handler — each is a one-line domain
/// call wrapped in the same load/catch/save shape, not enough distinct behavior to warrant
/// three separate classes.</summary>
public sealed class ProgramStatusHandler(DbContext dbContext, IAuditLogger auditLogger)
{
    public Task PublishAsync(Guid programId, Guid actorId, CancellationToken cancellationToken) =>
        TransitionAsync(programId, actorId, p => p.Publish(actorId), AuditActions.ContentPublished, cancellationToken);

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

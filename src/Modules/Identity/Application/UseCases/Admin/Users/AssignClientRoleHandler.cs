using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Application.UseCases.Admin.Users;

public sealed class AssignClientRoleHandler(DbContext dbContext, IAuditLogger auditLogger)
{
    public async Task HandleAsync(AssignClientRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == command.TargetUserId, cancellationToken)
            ?? throw new NotFoundAppException("The specified client does not exist.");

        var role = await dbContext.Set<Role>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken)
            ?? throw new NotFoundAppException("The specified role does not exist.");

        var alreadyAssigned = user.UserRoles.Any(ur => ur.RoleId == role.Id);

        user.AssignRole(role.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (alreadyAssigned)
        {
            // Idempotent no-op: still a legitimate admin action, but nothing changed — no audit
            // entry for a state transition that didn't happen.
            return;
        }

        await auditLogger.LogAsync(
            AuditEntry.Create(
                AuditActions.UserRoleChanged,
                actorUserId: command.ActorUserId,
                entityType: "User",
                entityId: command.TargetUserId.ToString(),
                metadata: new Dictionary<string, string> { ["role"] = role.Name, ["change"] = "assigned" }),
            cancellationToken);
    }
}

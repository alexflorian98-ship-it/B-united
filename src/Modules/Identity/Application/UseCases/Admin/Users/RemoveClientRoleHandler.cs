using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Application.UseCases.Admin.Users;

public sealed class RemoveClientRoleHandler(DbContext dbContext, IAuditLogger auditLogger)
{
    public async Task HandleAsync(RemoveClientRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == command.TargetUserId, cancellationToken)
            ?? throw new NotFoundAppException("The specified client does not exist.");

        var role = await dbContext.Set<Role>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken)
            ?? throw new NotFoundAppException("The specified role does not exist.");

        var isAssigned = user.UserRoles.Any(ur => ur.RoleId == role.Id);
        if (!isAssigned)
        {
            // Idempotent no-op, mirroring AssignClientRoleHandler.
            return;
        }

        if (role.Id == WellKnownRoles.AdministratorId)
        {
            var administratorCount = await dbContext.Set<UserRole>().AsNoTracking()
                .CountAsync(ur => ur.RoleId == WellKnownRoles.AdministratorId, cancellationToken);

            if (administratorCount <= 1)
            {
                throw new BusinessRuleAppException(
                    "LAST_ADMINISTRATOR_PROTECTED",
                    "errors.identity.lastAdministratorProtected",
                    "The last remaining Administrator's role cannot be removed.");
            }
        }

        user.RemoveRole(role.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditEntry.Create(
                AuditActions.UserRoleChanged,
                actorUserId: command.ActorUserId,
                entityType: "User",
                entityId: command.TargetUserId.ToString(),
                metadata: new Dictionary<string, string> { ["role"] = role.Name, ["change"] = "removed" }),
            cancellationToken);
    }
}

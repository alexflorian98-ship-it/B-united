using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Migrations.Seed;

/// <summary>
/// Idempotent seed for the roles/permissions from docs/ARCHITECTURE.md §14 (P1.16–P1.17).
/// Safe to run on every application startup — every insert is guarded by an existence check
/// against the fixed <see cref="WellKnownRoles"/>/<see cref="WellKnownPermissions"/> IDs.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(context, cancellationToken);
        await SeedPermissionsAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await SeedRolePermissionGrantsAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken)
    {
        var existingIds = (await context.Roles.Select(r => r.Id).ToListAsync(cancellationToken)).ToHashSet();

        (Guid Id, string Name)[] seedRoles =
        [
            (WellKnownRoles.ClientId, WellKnownRoles.Client),
            (WellKnownRoles.ExpertId, WellKnownRoles.Expert),
            (WellKnownRoles.AdministratorId, WellKnownRoles.Administrator),
        ];

        foreach (var (id, name) in seedRoles)
        {
            if (existingIds.Add(id))
            {
                context.Roles.Add(new Role(id, name));
            }
        }
    }

    private static async Task SeedPermissionsAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken)
    {
        var existingIds = (await context.Permissions.Select(p => p.Id).ToListAsync(cancellationToken)).ToHashSet();

        foreach (var (id, key, description) in WellKnownPermissions.All)
        {
            if (existingIds.Add(id))
            {
                context.Permissions.Add(new Permission(id, key, description));
            }
        }
    }

    private static async Task SeedRolePermissionGrantsAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken)
    {
        var existingGrants = (await context.RolePermissions
                .Select(rp => new { rp.RoleId, rp.PermissionId })
                .ToListAsync(cancellationToken))
            .Select(g => (g.RoleId, g.PermissionId))
            .ToHashSet();

        var permissionIdByKey = WellKnownPermissions.All.ToDictionary(p => p.Key, p => p.Id);

        foreach (var (roleId, permissionKeys) in RolePermissionMatrix)
        {
            foreach (var key in permissionKeys)
            {
                var permissionId = permissionIdByKey[key];
                if (existingGrants.Add((roleId, permissionId)))
                {
                    context.RolePermissions.Add(new RolePermission(roleId, permissionId));
                }
            }
        }
    }

    /// <summary>The Client/Expert/Administrator × permission grid from docs/ARCHITECTURE.md §14.</summary>
    private static readonly IReadOnlyList<(Guid RoleId, string[] PermissionKeys)> RolePermissionMatrix =
    [
        (WellKnownRoles.ClientId,
        [
            WellKnownPermissions.ContentView,
            WellKnownPermissions.QuestionnaireSubmit,
            WellKnownPermissions.EventsView,
            WellKnownPermissions.ChatUse,
            WellKnownPermissions.BillingView,
        ]),
        (WellKnownRoles.ExpertId,
        [
            WellKnownPermissions.ContentView,
            WellKnownPermissions.ContentCreate,
            WellKnownPermissions.ContentEdit,
            WellKnownPermissions.ContentPublish,
            WellKnownPermissions.QuestionnaireReview,
            WellKnownPermissions.QuestionnaireAnswer,
            WellKnownPermissions.EventsView,
            WellKnownPermissions.EventsManage,
            WellKnownPermissions.ChatUse,
            WellKnownPermissions.ChatModerate,
            WellKnownPermissions.BillingView,
        ]),
        (WellKnownRoles.AdministratorId,
        [
            WellKnownPermissions.ContentView,
            WellKnownPermissions.ContentCreate,
            WellKnownPermissions.ContentEdit,
            WellKnownPermissions.ContentPublish,
            WellKnownPermissions.EventsView,
            WellKnownPermissions.EventsManage,
            WellKnownPermissions.ChatUse,
            WellKnownPermissions.ChatModerate,
            WellKnownPermissions.BillingView,
            WellKnownPermissions.BillingManage,
            WellKnownPermissions.BillingViewRawWebhookPayloads,
            WellKnownPermissions.UsersManage,
            WellKnownPermissions.AuditView,
        ]),
    ];
}

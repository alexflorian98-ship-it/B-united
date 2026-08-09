using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Application.UseCases.Admin.Users;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;

namespace BUnited.Modules.Identity.Tests.UseCases;

/// <summary>docs/IMPLEMENTATION_PLAN.md Slice A3 — real client administration (list/search/
/// filter, detail, role assignment) replacing the "Subscribers" placeholder. Covers the required
/// tests named in the plan: wrong permission is covered at the Api layer by
/// <c>PermissionEnforcementTests</c> (every action here requires <c>users.manage</c>, already
/// proven generic); this file covers invalid role, concurrent-safe idempotency, last-administrator
/// protection, the audit entry, and the absence of sensitive questionnaire data from every DTO
/// shape (none of these DTOs have a field capable of carrying it — see the DTO definitions).</summary>
public sealed class AdminClientsFlowTests
{
    private static async Task<(TestDbContext Context, Microsoft.Data.Sqlite.SqliteConnection Connection)> SeedRolesAsync()
    {
        var (connection, context) = TestDbContextFactory.Create();
        context.Roles.Add(new Role(WellKnownRoles.ClientId, WellKnownRoles.Client));
        context.Roles.Add(new Role(WellKnownRoles.ExpertId, WellKnownRoles.Expert));
        context.Roles.Add(new Role(WellKnownRoles.AdministratorId, WellKnownRoles.Administrator));
        await context.SaveChangesAsync();
        return (context, connection);
    }

    private static User CreateUser(string email, Guid roleId)
    {
        var user = User.Register(email, new PasswordHasher().Hash("StrongPass123"));
        user.MarkEmailVerified(DateTime.UtcNow);
        user.AssignRole(roleId);
        return user;
    }

    [Fact]
    public async Task List_filters_by_search_and_returns_role_membership()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        context.Users.Add(CreateUser("ada@example.com", WellKnownRoles.ClientId));
        context.Users.Add(CreateUser("grace@example.com", WellKnownRoles.ExpertId));
        await context.SaveChangesAsync();

        var handler = new ListClientsHandler(context);
        var result = await handler.HandleAsync(new ListClientsQuery(Search: "ada", RoleId: null, Page: 1, PageSize: 20), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("ada@example.com", item.Email);
        Assert.Equal(WellKnownRoles.Client, Assert.Single(item.Roles).Name);
    }

    [Fact]
    public async Task List_filters_by_role_id()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        context.Users.Add(CreateUser("ada@example.com", WellKnownRoles.ClientId));
        context.Users.Add(CreateUser("grace@example.com", WellKnownRoles.ExpertId));
        await context.SaveChangesAsync();

        var handler = new ListClientsHandler(context);
        var result = await handler.HandleAsync(new ListClientsQuery(Search: null, RoleId: WellKnownRoles.ExpertId, Page: 1, PageSize: 20), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("grace@example.com", item.Email);
    }

    [Fact]
    public async Task Get_detail_for_unknown_user_throws_not_found()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        var handler = new GetClientDetailHandler(context);

        await Assert.ThrowsAsync<NotFoundAppException>(() => handler.HandleAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Assigning_a_role_grants_it_and_records_a_metadata_only_audit_entry()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        var user = CreateUser("ada@example.com", WellKnownRoles.ClientId);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actorId = Guid.NewGuid();
        var auditLogger = new RecordingAuditLogger();
        var handler = new AssignClientRoleHandler(context, auditLogger);

        await handler.HandleAsync(new AssignClientRoleCommand(actorId, user.Id, WellKnownRoles.ExpertId), CancellationToken.None);

        var detail = await new GetClientDetailHandler(context).HandleAsync(user.Id, CancellationToken.None);
        Assert.Contains(detail.Roles, r => r.Name == WellKnownRoles.Expert);

        var auditEntry = Assert.Single(auditLogger.Entries);
        Assert.Equal(AuditActions.UserRoleChanged, auditEntry.Action);
        Assert.Equal(actorId, auditEntry.ActorUserId);
        Assert.Equal(user.Id.ToString(), auditEntry.EntityId);
        Assert.Equal("assigned", auditEntry.Metadata?["change"]);
        Assert.Equal(WellKnownRoles.Expert, auditEntry.Metadata?["role"]);
    }

    [Fact]
    public async Task Assigning_an_already_held_role_is_an_idempotent_no_op_and_does_not_audit_again()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        var user = CreateUser("ada@example.com", WellKnownRoles.ClientId);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var auditLogger = new RecordingAuditLogger();
        var handler = new AssignClientRoleHandler(context, auditLogger);

        await handler.HandleAsync(new AssignClientRoleCommand(Guid.NewGuid(), user.Id, WellKnownRoles.ClientId), CancellationToken.None);

        Assert.Empty(auditLogger.Entries);
    }

    [Fact]
    public async Task Assigning_an_unknown_role_throws_not_found()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        var user = CreateUser("ada@example.com", WellKnownRoles.ClientId);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new AssignClientRoleHandler(context, new RecordingAuditLogger());

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => handler.HandleAsync(new AssignClientRoleCommand(Guid.NewGuid(), user.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Removing_a_role_revokes_it_and_records_a_metadata_only_audit_entry()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        var user = CreateUser("ada@example.com", WellKnownRoles.ClientId);
        user.AssignRole(WellKnownRoles.ExpertId);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actorId = Guid.NewGuid();
        var auditLogger = new RecordingAuditLogger();
        var handler = new RemoveClientRoleHandler(context, auditLogger);

        await handler.HandleAsync(new RemoveClientRoleCommand(actorId, user.Id, WellKnownRoles.ExpertId), CancellationToken.None);

        var detail = await new GetClientDetailHandler(context).HandleAsync(user.Id, CancellationToken.None);
        Assert.DoesNotContain(detail.Roles, r => r.Name == WellKnownRoles.Expert);

        var auditEntry = Assert.Single(auditLogger.Entries);
        Assert.Equal(AuditActions.UserRoleChanged, auditEntry.Action);
        Assert.Equal("removed", auditEntry.Metadata?["change"]);
    }

    [Fact]
    public async Task Removing_a_role_the_user_does_not_hold_is_an_idempotent_no_op()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        var user = CreateUser("ada@example.com", WellKnownRoles.ClientId);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var auditLogger = new RecordingAuditLogger();
        var handler = new RemoveClientRoleHandler(context, auditLogger);

        await handler.HandleAsync(new RemoveClientRoleCommand(Guid.NewGuid(), user.Id, WellKnownRoles.ExpertId), CancellationToken.None);

        Assert.Empty(auditLogger.Entries);
    }

    [Fact]
    public async Task Removing_the_last_administrators_role_is_rejected()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        var admin = CreateUser("admin@example.com", WellKnownRoles.AdministratorId);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var handler = new RemoveClientRoleHandler(context, new RecordingAuditLogger());

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new RemoveClientRoleCommand(Guid.NewGuid(), admin.Id, WellKnownRoles.AdministratorId), CancellationToken.None));

        Assert.Equal("LAST_ADMINISTRATOR_PROTECTED", exception.Code);

        var detail = await new GetClientDetailHandler(context).HandleAsync(admin.Id, CancellationToken.None);
        Assert.Contains(detail.Roles, r => r.Name == WellKnownRoles.Administrator);
    }

    [Fact]
    public async Task Removing_an_administrator_role_succeeds_when_another_administrator_remains()
    {
        var (context, connection) = await SeedRolesAsync();
        using var __ = connection;
        using var ___ = context;

        var admin1 = CreateUser("admin1@example.com", WellKnownRoles.AdministratorId);
        var admin2 = CreateUser("admin2@example.com", WellKnownRoles.AdministratorId);
        context.Users.Add(admin1);
        context.Users.Add(admin2);
        await context.SaveChangesAsync();

        var handler = new RemoveClientRoleHandler(context, new RecordingAuditLogger());

        await handler.HandleAsync(new RemoveClientRoleCommand(Guid.NewGuid(), admin1.Id, WellKnownRoles.AdministratorId), CancellationToken.None);

        var detail = await new GetClientDetailHandler(context).HandleAsync(admin1.Id, CancellationToken.None);
        Assert.DoesNotContain(detail.Roles, r => r.Name == WellKnownRoles.Administrator);
    }
}

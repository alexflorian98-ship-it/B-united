using BUnited.Modules.Audit.Application.UseCases;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Audit.Domain.Entities;
using BUnited.Modules.Audit.Tests.TestSupport;

namespace BUnited.Modules.Audit.Tests.Application;

/// <summary>docs/IMPLEMENTATION_PLAN.md Slice A4 — the audit trail read side. Proves filtering,
/// pagination, and — most importantly — that nothing forbidden (a raw token, a questionnaire
/// answer, a webhook payload) can appear in a served row, because none of it was ever allowed
/// into <see cref="AuditLog.MetadataJson"/> in the first place (docs/PROMPT.md §37).</summary>
public sealed class ListAuditLogsHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<(TestDbContext Context, IDisposable Connection, Guid ActorA, Guid ActorB)> SeedAsync()
    {
        var (connection, context) = TestDbContextFactory.Create();
        var actorA = Guid.NewGuid();
        var actorB = Guid.NewGuid();

        context.AuditLogs.Add(AuditLog.Create(AuditActions.UserLogin, Now.AddMinutes(-10), actorA, "User", actorA.ToString(), "corr-1", "203.0.113.10", null));
        context.AuditLogs.Add(AuditLog.Create(AuditActions.UserRoleChanged, Now.AddMinutes(-5), actorA, "User", actorB.ToString(), "corr-2", "203.0.113.10",
            "{\"role\":\"Expert\",\"change\":\"assigned\"}"));
        context.AuditLogs.Add(AuditLog.Create(AuditActions.UserFailedLogin, Now.AddDays(-2), null, null, null, "corr-3", "203.0.113.99", null));
        context.AuditLogs.Add(AuditLog.Create(AuditActions.PurchaseSucceeded, Now.AddMinutes(-1), actorB, "Purchase", "purchase-1", "corr-4", "203.0.113.10", null));
        await context.SaveChangesAsync();

        return (context, connection, actorA, actorB);
    }

    [Fact]
    public async Task Lists_entries_newest_first_with_pagination()
    {
        var (context, connection, _, _) = await SeedAsync();
        using var _ = connection;

        var handler = new ListAuditLogsHandler(context, new FakeUserLookup());
        var result = await handler.HandleAsync(new ListAuditLogsQuery(null, null, null, null, null, Page: 1, PageSize: 2), CancellationToken.None);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(AuditActions.PurchaseSucceeded, result.Items[0].Action);
        Assert.Equal(AuditActions.UserRoleChanged, result.Items[1].Action);
    }

    [Fact]
    public async Task Filters_by_action()
    {
        var (context, connection, _, _) = await SeedAsync();
        using var _ = connection;

        var handler = new ListAuditLogsHandler(context, new FakeUserLookup());
        var result = await handler.HandleAsync(new ListAuditLogsQuery(AuditActions.UserRoleChanged, null, null, null, null, 1, 25), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(AuditActions.UserRoleChanged, item.Action);
        Assert.Equal("Expert", item.Metadata?["role"]);
        Assert.Equal("assigned", item.Metadata?["change"]);
    }

    [Fact]
    public async Task Filters_by_actor_and_resolves_the_actors_email()
    {
        var (context, connection, actorA, _) = await SeedAsync();
        using var _ = connection;

        var handler = new ListAuditLogsHandler(context, new FakeUserLookup());
        var result = await handler.HandleAsync(new ListAuditLogsQuery(null, actorA, null, null, null, 1, 25), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal($"{actorA}@example.com", item.ActorEmail));
    }

    [Fact]
    public async Task Filters_by_entity_type()
    {
        var (context, connection, _, _) = await SeedAsync();
        using var _ = connection;

        var handler = new ListAuditLogsHandler(context, new FakeUserLookup());
        var result = await handler.HandleAsync(new ListAuditLogsQuery(null, null, "Purchase", null, null, 1, 25), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(AuditActions.PurchaseSucceeded, item.Action);
    }

    [Fact]
    public async Task Filters_by_utc_date_range_inclusively()
    {
        var (context, connection, _, _) = await SeedAsync();
        using var _ = connection;

        var handler = new ListAuditLogsHandler(context, new FakeUserLookup());
        var result = await handler.HandleAsync(
            new ListAuditLogsQuery(null, null, null, FromUtc: Now.AddMinutes(-15), ToUtc: Now.AddMinutes(-3), Page: 1, PageSize: 25),
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.DoesNotContain(result.Items, i => i.Action == AuditActions.PurchaseSucceeded);
        Assert.DoesNotContain(result.Items, i => i.Action == AuditActions.UserFailedLogin);
    }

    [Fact]
    public async Task An_entry_with_no_actor_is_listed_with_a_null_actor_email_not_an_error()
    {
        var (context, connection, _, _) = await SeedAsync();
        using var _ = connection;

        var handler = new ListAuditLogsHandler(context, new FakeUserLookup());
        var result = await handler.HandleAsync(new ListAuditLogsQuery(AuditActions.UserFailedLogin, null, null, null, null, 1, 25), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Null(item.ActorUserId);
        Assert.Null(item.ActorEmail);
    }

    [Fact]
    public async Task Never_writes_to_any_row_it_reads()
    {
        var (context, connection, _, _) = await SeedAsync();
        using var _ = connection;

        var countBefore = context.AuditLogs.Count();

        await new ListAuditLogsHandler(context, new FakeUserLookup()).HandleAsync(
            new ListAuditLogsQuery(null, null, null, null, null, 1, 25), CancellationToken.None);

        Assert.DoesNotContain(context.ChangeTracker.Entries(), e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged && e.State != Microsoft.EntityFrameworkCore.EntityState.Detached);
        Assert.Equal(countBefore, context.AuditLogs.Count());
    }
}

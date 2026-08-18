using BUnited.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Tests.TestSupport;

internal static class TestDbContextFactory
{
    public static (SqliteConnection Connection, TestDbContext Context) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor())
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();

        return (connection, context);
    }

    /// <summary>Mirrors <c>Billing.Tests.TestSupport.TestDbContextFactory.CreateConcurrentPair</c>
    /// — two independent connections to the same named, shared-cache in-memory database, so two
    /// <see cref="TestDbContext"/> instances on two different threads can race real, concurrent
    /// <c>SaveChanges</c> calls against the same schema and data instead of serializing through a
    /// single shared connection. SQLite's own engine-level locking (with a busy-timeout so a
    /// losing writer waits and retries instead of failing with "database is locked") then
    /// serializes the two commits the way Postgres would, letting the second writer hit the real
    /// unique-index violation on <c>(UserId, SectionId)</c>.</summary>
    public static (SqliteConnection Connection1, TestDbContext Context1, SqliteConnection Connection2, TestDbContext Context2) CreateConcurrentPair()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"concurrency-test-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        var connection1 = OpenWithBusyTimeout(connectionString);
        var options1 = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection1)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor())
            .Options;
        var context1 = new TestDbContext(options1);
        context1.Database.EnsureCreated();

        var connection2 = OpenWithBusyTimeout(connectionString);
        var options2 = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection2)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor())
            .Options;
        var context2 = new TestDbContext(options2);

        return (connection1, context1, connection2, context2);
    }

    private static SqliteConnection OpenWithBusyTimeout(string connectionString)
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA busy_timeout = 5000;";
        pragma.ExecuteNonQuery();

        return connection;
    }
}

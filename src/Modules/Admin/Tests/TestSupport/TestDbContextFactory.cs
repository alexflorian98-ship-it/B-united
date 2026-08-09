using BUnited.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Admin.Tests.TestSupport;

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
}

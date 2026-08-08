using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BUnited.Migrations;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations add/update</c>, which cannot resolve
/// the DbContext through the Api host's DI container. Loads the connection string the same way
/// the Api host does (repo-root <c>.env</c>, see README.md "Local development setup").
/// </summary>
public sealed class BUnitedApplicationDbContextFactory : IDesignTimeDbContextFactory<BUnitedApplicationDbContext>
{
    public BUnitedApplicationDbContext CreateDbContext(string[] args)
    {
        Env.TraversePath().Load();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__Default is not set. Set it in the repo-root .env before running EF Core design-time tools.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<BUnitedApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(BUnitedApplicationDbContext).Assembly.FullName));

        return new BUnitedApplicationDbContext(optionsBuilder.Options);
    }
}

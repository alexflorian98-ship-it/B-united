using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace BUnited.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base <see cref="DbContext"/> shared by all module DbContexts. Auto-registers every
/// <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/> found in the
/// supplied module assemblies and applies the project-wide snake_case naming convention
/// (PostgreSQL, see docs/ARCHITECTURE.md §5) to tables, columns, keys, foreign keys and indexes.
/// </summary>
public abstract class BUnitedDbContext : DbContext
{
    private readonly IReadOnlyCollection<Assembly> _configurationAssemblies;

    protected BUnitedDbContext(DbContextOptions options, IEnumerable<Assembly> configurationAssemblies)
        : base(options)
    {
        _configurationAssemblies = configurationAssemblies.ToArray();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var assembly in _configurationAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        ApplySnakeCaseNamingConvention(modelBuilder);
    }

    private static void ApplySnakeCaseNamingConvention(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetTableName(ToSnakeCase(entityType.GetTableName()!));

            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entityType.GetKeys())
            {
                var name = key.GetName();
                if (name is not null)
                {
                    key.SetName(ToSnakeCase(name));
                }
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var name = foreignKey.GetConstraintName();
                if (name is not null)
                {
                    foreignKey.SetConstraintName(ToSnakeCase(name));
                }
            }

            foreach (var index in entityType.GetIndexes())
            {
                var name = index.GetDatabaseName();
                if (name is not null)
                {
                    index.SetDatabaseName(ToSnakeCase(name));
                }
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (char.IsUpper(current))
            {
                var isNotFirstCharacter = i > 0;
                var previousIsLower = isNotFirstCharacter && char.IsLower(value[i - 1]);
                var nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);

                if (isNotFirstCharacter && (previousIsLower || nextIsLower))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}

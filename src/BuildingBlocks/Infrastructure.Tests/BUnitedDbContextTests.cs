using BUnited.BuildingBlocks.Domain;
using BUnited.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BUnited.BuildingBlocks.Infrastructure.Tests;

public sealed class BUnitedDbContextTests
{
    private sealed class SampleEntity : IAuditableEntity
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    private sealed class SampleEntityConfiguration : IEntityTypeConfiguration<SampleEntity>
    {
        public void Configure(EntityTypeBuilder<SampleEntity> builder)
        {
            builder.Property(x => x.FirstName).HasMaxLength(200);
        }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : BUnitedDbContext(options, new[] { typeof(TestDbContext).Assembly })
    {
        public DbSet<SampleEntity> SampleEntities => Set<SampleEntity>();
    }

    private static (SqliteConnection Connection, TestDbContext Context) CreateContext(
        params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection);
        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        var context = new TestDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return (connection, context);
    }

    [Fact]
    public void Applies_snake_case_table_and_column_names()
    {
        var (connection, context) = CreateContext();
        using var _ = connection;
        using var __ = context;

        var entityType = context.Model.FindEntityType(typeof(SampleEntity))!;

        Assert.Equal("sample_entities", entityType.GetTableName());
        Assert.Equal("first_name", entityType.FindProperty(nameof(SampleEntity.FirstName))!.GetColumnName());
    }

    [Fact]
    public void Auto_registers_IEntityTypeConfiguration_implementations_from_supplied_assembly()
    {
        var (connection, context) = CreateContext();
        using var _ = connection;
        using var __ = context;

        var property = context.Model.FindEntityType(typeof(SampleEntity))!
            .FindProperty(nameof(SampleEntity.FirstName))!;

        Assert.Equal(200, property.GetMaxLength());
    }

    [Fact]
    public async Task Interceptor_sets_created_and_updated_at_on_insert()
    {
        var (connection, context) = CreateContext(new AuditableEntitySaveChangesInterceptor());
        using var _ = connection;
        using var __ = context;

        var entity = new SampleEntity { FirstName = "Ada" };
        context.SampleEntities.Add(entity);
        await context.SaveChangesAsync();

        Assert.NotEqual(default, entity.CreatedAt);
        Assert.Equal(entity.CreatedAt, entity.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, entity.CreatedAt.Kind);
    }

    [Fact]
    public async Task Interceptor_updates_only_updated_at_on_modification()
    {
        var (connection, context) = CreateContext(new AuditableEntitySaveChangesInterceptor());
        using var _ = connection;
        using var __ = context;

        var entity = new SampleEntity { FirstName = "Ada" };
        context.SampleEntities.Add(entity);
        await context.SaveChangesAsync();
        var originalCreatedAt = entity.CreatedAt;

        await Task.Delay(10);
        entity.FirstName = "Grace";
        await context.SaveChangesAsync();

        Assert.Equal(originalCreatedAt, entity.CreatedAt);
        Assert.True(entity.UpdatedAt > originalCreatedAt);
    }
}

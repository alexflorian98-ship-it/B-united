using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Chat.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Content.Domain.Entities;
using BUnited.Modules.Content.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Events.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Questionnaires.Domain.Entities;
using BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Admin.Tests.TestSupport;

/// <summary>Combines the Sqlite-friendly configuration assemblies of every module
/// <see cref="Application.UseCases.GetDashboardHandler"/> reads from — the same set that
/// <see cref="BUnited.Migrations.BUnitedApplicationDbContext"/> combines in production, minus
/// Identity/Progress/Audit (not read by the dashboard). This mirrors each module's own
/// TestDbContext exactly (see e.g. Billing.Tests/TestSupport/TestDbContext.cs) rather than
/// inventing a new pattern.</summary>
internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : BUnitedDbContext(options,
    [
        typeof(ProgramConfiguration).Assembly,
        typeof(QuestionnaireConfiguration).Assembly,
        typeof(ProgramOfferConfiguration).Assembly,
        typeof(EventConfiguration).Assembly,
        typeof(MessageConfiguration).Assembly,
    ])
{
    /// <summary>Same Sqlite xmin workaround as Content/Billing/Events' own TestDbContexts —
    /// these entities' concurrency tokens map to Postgres's native <c>xmin</c> column, which
    /// Sqlite has no equivalent for.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Program>().Property<uint>("xmin").ValueGeneratedNever().HasDefaultValue(0u);
        modelBuilder.Entity<Event>().Property<uint>("xmin").ValueGeneratedNever().HasDefaultValue(0u);
        modelBuilder.Entity<EventRegistration>().Property<uint>("xmin").ValueGeneratedNever().HasDefaultValue(0u);
        modelBuilder.Entity<ProgramOffer>().Property<uint>("xmin").ValueGeneratedNever().HasDefaultValue(0u);
    }

    // Content
    public DbSet<ContentDomain> ContentDomains => Set<ContentDomain>();

    public DbSet<Program> Programs => Set<Program>();

    // Questionnaires
    public DbSet<Questionnaire> Questionnaires => Set<Questionnaire>();

    public DbSet<QuestionnaireSubmission> QuestionnaireSubmissions => Set<QuestionnaireSubmission>();

    // Billing
    public DbSet<ProgramOffer> ProgramOffers => Set<ProgramOffer>();

    public DbSet<ProgramPrice> ProgramPrices => Set<ProgramPrice>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<ProgramEntitlement> ProgramEntitlements => Set<ProgramEntitlement>();

    // Events
    public DbSet<Event> Events => Set<Event>();

    public DbSet<EventTranslation> EventTranslations => Set<EventTranslation>();

    // Chat
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Report> Reports => Set<Report>();
}

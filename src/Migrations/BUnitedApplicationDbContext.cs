using System.Reflection;
using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Audit.Domain.Entities;
using BUnited.Modules.Audit.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Billing.Domain.Entities;
using BUnited.Modules.Billing.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Chat.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Content.Domain.Entities;
using BUnited.Modules.Content.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Events.Domain.Entities;
using BUnited.Modules.Events.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Progress.Domain.Entities;
using BUnited.Modules.Progress.Infrastructure.Persistence.Configurations;
using BUnited.Modules.Questionnaires.Domain.Entities;
using BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Migrations;

/// <summary>
/// The single composed <see cref="DbContext"/> for the whole modular monolith (one PostgreSQL
/// database, per ADR-002). Each module owns its entities and <c>IEntityTypeConfiguration&lt;T&gt;</c>
/// classes in its own Domain/Infrastructure layers; this type only aggregates their assemblies
/// and exposes the resulting <see cref="DbSet{TEntity}"/>s. Add a module's Infrastructure
/// assembly to <see cref="ConfigurationAssemblies"/> and its `DbSet`s here as new modules land.
/// </summary>
public sealed class BUnitedApplicationDbContext : BUnitedDbContext
{
    private static readonly Assembly[] ConfigurationAssemblies =
    [
        typeof(UserConfiguration).Assembly,
        typeof(AuditLogConfiguration).Assembly,
        typeof(ProgramConfiguration).Assembly,
        typeof(ContentProgressConfiguration).Assembly,
        typeof(QuestionnaireConfiguration).Assembly,
        typeof(ProgramOfferConfiguration).Assembly,
        typeof(EventConfiguration).Assembly,
        typeof(MessageConfiguration).Assembly,
    ];

    public BUnitedApplicationDbContext(DbContextOptions<BUnitedApplicationDbContext> options)
        : base(options, ConfigurationAssemblies)
    {
    }

    // Identity
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<UserConsent> UserConsents => Set<UserConsent>();

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Content
    public DbSet<ContentDomain> ContentDomains => Set<ContentDomain>();

    public DbSet<Program> Programs => Set<Program>();

    public DbSet<ProgramTranslation> ProgramTranslations => Set<ProgramTranslation>();

    public DbSet<Section> Sections => Set<Section>();

    public DbSet<SectionTranslation> SectionTranslations => Set<SectionTranslation>();

    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

    public DbSet<ContentItemTranslation> ContentItemTranslations => Set<ContentItemTranslation>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();

    public DbSet<QuizQuestionTranslation> QuizQuestionTranslations => Set<QuizQuestionTranslation>();

    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();

    public DbSet<QuizOptionTranslation> QuizOptionTranslations => Set<QuizOptionTranslation>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    // Progress
    public DbSet<ContentProgress> ContentProgressEntries => Set<ContentProgress>();

    public DbSet<SectionProgress> SectionProgressEntries => Set<SectionProgress>();

    // Questionnaires
    public DbSet<Questionnaire> Questionnaires => Set<Questionnaire>();

    public DbSet<QuestionnaireTranslation> QuestionnaireTranslations => Set<QuestionnaireTranslation>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<QuestionTranslation> QuestionTranslations => Set<QuestionTranslation>();

    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();

    public DbSet<QuestionOptionTranslation> QuestionOptionTranslations => Set<QuestionOptionTranslation>();

    public DbSet<QuestionnaireSubmission> QuestionnaireSubmissions => Set<QuestionnaireSubmission>();

    public DbSet<QuestionnaireAnswer> QuestionnaireAnswers => Set<QuestionnaireAnswer>();

    public DbSet<GuidanceResponse> GuidanceResponses => Set<GuidanceResponse>();

    public DbSet<GuidanceFollowUp> GuidanceFollowUps => Set<GuidanceFollowUp>();

    // Billing
    public DbSet<ProgramOffer> ProgramOffers => Set<ProgramOffer>();

    public DbSet<ProgramPrice> ProgramPrices => Set<ProgramPrice>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<ProgramEntitlement> ProgramEntitlements => Set<ProgramEntitlement>();

    public DbSet<PaymentCustomer> PaymentCustomers => Set<PaymentCustomer>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

    // Events
    public DbSet<Event> Events => Set<Event>();

    public DbSet<EventTranslation> EventTranslations => Set<EventTranslation>();

    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    public DbSet<EventReminder> EventReminders => Set<EventReminder>();

    public DbSet<EventProgram> EventPrograms => Set<EventProgram>();

    // Chat
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<Mute> Mutes => Set<Mute>();

    public DbSet<ChatReadState> ChatReadStates => Set<ChatReadState>();
}

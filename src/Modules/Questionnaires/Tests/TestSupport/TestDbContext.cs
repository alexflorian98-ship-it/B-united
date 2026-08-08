using BUnited.BuildingBlocks.Infrastructure.Persistence;
using BUnited.Modules.Questionnaires.Domain.Entities;
using BUnited.Modules.Questionnaires.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Tests.TestSupport;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : BUnitedDbContext(options, [typeof(QuestionnaireConfiguration).Assembly])
{
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
}

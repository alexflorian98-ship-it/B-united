using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Questionnaires.Application.UseCases.Admin;
using BUnited.Modules.Questionnaires.Application.UseCases.Client;
using BUnited.Modules.Questionnaires.Application.UseCases.DataRights;
using BUnited.Modules.Questionnaires.Application.UseCases.Expert;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Questionnaires.Infrastructure;

public static class QuestionnairesModuleExtensions
{
    public static IServiceCollection AddQuestionnairesModule(this IServiceCollection services)
    {
        // Admin/builder
        services.AddScoped<CreateQuestionnaireHandler>();
        services.AddScoped<UpsertQuestionnaireTranslationHandler>();
        services.AddScoped<QuestionnaireStatusHandler>();
        services.AddScoped<AddQuestionHandler>();
        services.AddScoped<UpsertQuestionTranslationHandler>();
        services.AddScoped<DeleteQuestionHandler>();
        services.AddScoped<ReorderQuestionsHandler>();
        services.AddScoped<AddQuestionOptionHandler>();
        services.AddScoped<UpsertQuestionOptionTranslationHandler>();
        services.AddScoped<DeleteQuestionOptionHandler>();
        services.AddScoped<GetQuestionnaireDetailHandler>();
        services.AddScoped<ListQuestionnairesHandler>();

        // Client
        services.AddScoped<RecordQuestionnaireConsentHandler>();
        services.AddScoped<ListPublishedQuestionnairesHandler>();
        services.AddScoped<GetClientQuestionnaireHandler>();
        services.AddScoped<StartOrResumeSubmissionHandler>();
        services.AddScoped<SaveDraftAnswersHandler>();
        services.AddScoped<GetMySubmissionHandler>();
        services.AddScoped<ListMySubmissionsHandler>();
        services.AddScoped<SubmitQuestionnaireHandler>();
        services.AddScoped<GetGuidanceHandler>();
        services.AddScoped<SubmitFollowUpHandler>();
        services.AddScoped<ExportMyQuestionnaireDataHandler>();
        services.AddScoped<IUserDataExporter, QuestionnairesUserDataExporter>();
        services.AddScoped<IUserDataEraser, QuestionnairesUserDataEraser>();

        // Expert
        services.AddScoped<GetQueueHandler>();
        services.AddScoped<GetSubmissionDetailForExpertHandler>();
        services.AddScoped<SaveGuidanceDraftHandler>();
        services.AddScoped<PublishGuidanceHandler>();
        services.AddScoped<AnswerFollowUpHandler>();

        services.AddValidatorsFromAssemblyContaining<CreateQuestionnaireValidator>();

        return services;
    }
}

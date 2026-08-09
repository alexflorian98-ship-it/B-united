using BUnited.Modules.Content.Application.Abstractions;
using BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;
using BUnited.Modules.Content.Application.UseCases.Admin.Programs;
using BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;
using BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;
using BUnited.Modules.Content.Application.UseCases.Admin.Sections;
using BUnited.Modules.Content.Application.UseCases.Client;
using BUnited.Modules.Content.Contracts;
using BUnited.Modules.Content.Infrastructure.CrossModule;
using BUnited.Modules.Content.Infrastructure.Video;
using BUnited.Modules.Progress.Contracts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Content.Infrastructure;

public static class ContentModuleExtensions
{
    public static IServiceCollection AddContentModule(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IVideoProvider, YouTubeVideoProvider>();
        services.AddScoped<IProgramLookup, ProgramLookup>();
        services.AddScoped<IContentItemProgramLookup, ContentItemProgramLookup>();

        services.AddScoped<CreateProgramHandler>();
        services.AddScoped<UpsertProgramTranslationHandler>();
        services.AddScoped<ProgramStatusHandler>();
        services.AddScoped<GetProgramDetailHandler>();
        services.AddScoped<ListProgramsHandler>();
        services.AddScoped<ReorderSectionsHandler>();

        services.AddScoped<AddSectionHandler>();
        services.AddScoped<UpsertSectionTranslationHandler>();
        services.AddScoped<DeleteSectionHandler>();
        services.AddScoped<ReorderContentItemsHandler>();

        services.AddScoped<AddContentItemHandler>();
        services.AddScoped<UpsertContentItemTranslationHandler>();
        services.AddScoped<DeleteContentItemHandler>();

        services.AddScoped<AddQuizQuestionHandler>();
        services.AddScoped<UpsertQuizQuestionTranslationHandler>();
        services.AddScoped<DeleteQuizQuestionHandler>();
        services.AddScoped<ReorderQuizQuestionsHandler>();
        services.AddScoped<AddQuizOptionHandler>();
        services.AddScoped<UpsertQuizOptionTranslationHandler>();
        services.AddScoped<DeleteQuizOptionHandler>();
        services.AddScoped<ReorderQuizOptionsHandler>();

        services.AddScoped<ListContentDomainsHandler>();
        services.AddScoped<ListPublishedProgramsHandler>();
        services.AddScoped<GetPublishedProgramDetailHandler>();
        services.AddScoped<GetVideoPlaybackHandler>();
        services.AddScoped<SubmitQuizAttemptHandler>();

        services.AddValidatorsFromAssemblyContaining<CreateProgramValidator>();

        return services;
    }
}

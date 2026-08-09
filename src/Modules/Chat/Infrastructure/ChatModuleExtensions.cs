using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Chat.Application.UseCases.Admin;
using BUnited.Modules.Chat.Application.UseCases.Client;
using BUnited.Modules.Chat.Application.UseCases.DataRights;
using BUnited.Modules.Chat.Application.UseCases.Moderation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Chat.Infrastructure;

public static class ChatModuleExtensions
{
    public static IServiceCollection AddChatModule(this IServiceCollection services)
    {
        // Client
        services.AddScoped<ListRoomsHandler>();
        services.AddScoped<GetMessagesHandler>();
        services.AddScoped<SendMessageHandler>();
        services.AddScoped<MarkRoomReadHandler>();
        services.AddScoped<ReportMessageHandler>();
        services.AddScoped<IUserDataExporter, ChatUserDataExporter>();
        services.AddScoped<IUserDataEraser, ChatUserDataEraser>();

        // Moderation
        services.AddScoped<PinMessageHandler>();
        services.AddScoped<DeleteMessageHandler>();
        services.AddScoped<MuteUserHandler>();
        services.AddScoped<ListReportsHandler>();
        services.AddScoped<ResolveReportHandler>();
        services.AddScoped<ListMutedUsersHandler>();
        services.AddScoped<ListRecentModeratorActionsHandler>();

        // Admin room management
        services.AddScoped<CreateChatRoomHandler>();
        services.AddScoped<UpdateChatRoomHandler>();
        services.AddScoped<ListChatRoomsAdminHandler>();

        services.AddValidatorsFromAssemblyContaining<SendMessageValidator>();

        return services;
    }
}

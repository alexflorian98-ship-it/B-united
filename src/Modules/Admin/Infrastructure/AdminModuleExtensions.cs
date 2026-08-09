using BUnited.Modules.Admin.Application.UseCases;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Admin.Infrastructure;

public static class AdminModuleExtensions
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<GetDashboardHandler>();
        services.AddScoped<GetClientCommerceSummaryHandler>();

        // The expert dashboard combines data owned by four modules (Questionnaires, Events,
        // Billing, Chat). Rather than minting a new granular permission, access is granted to
        // whichever staff member already holds at least one of the permissions each widget's
        // source module already gates on — the same population that today reaches the expert
        // queue, admin billing, admin events, or chat moderation screens individually.
        services.AddAuthorization(options => options.AddPolicy(
            AdminPermissionPolicies.DashboardView,
            policy => policy.RequireAssertion(context =>
                context.User.HasClaim("permission", WellKnownPermissionKeys.QuestionnaireReview) ||
                context.User.HasClaim("permission", WellKnownPermissionKeys.BillingManage) ||
                context.User.HasClaim("permission", WellKnownPermissionKeys.EventsManage) ||
                context.User.HasClaim("permission", WellKnownPermissionKeys.ChatModerate))));

        return services;
    }
}

/// <summary>Policy names owned by the Admin module itself (not mirrored from Identity's
/// well-known permission set, because this is a composite "any of" policy over existing
/// permissions, not a new grantable permission — see <see cref="AddAdminModule"/>).</summary>
public static class AdminPermissionPolicies
{
    public const string DashboardView = "admin.dashboard.view";
}

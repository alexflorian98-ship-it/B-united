using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Application.UseCases.Login;
using BUnited.Modules.Identity.Application.UseCases.PasswordReset;
using BUnited.Modules.Identity.Application.UseCases.Profile;
using BUnited.Modules.Identity.Application.UseCases.Refresh;
using BUnited.Modules.Identity.Application.UseCases.Register;
using BUnited.Modules.Identity.Application.UseCases.Revoke;
using BUnited.Modules.Identity.Application.UseCases.VerifyEmail;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Identity.Infrastructure.CrossModule;
using BUnited.Modules.Identity.Infrastructure.Notifications;
using BUnited.Modules.Identity.Infrastructure.Security;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BUnited.Modules.Identity.Infrastructure;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AccountLockoutOptions>(configuration.GetSection(AccountLockoutOptions.SectionName));

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IIdentityEmailSender, LoggingIdentityEmailSender>();
        services.AddScoped<IUserLookup, IdentityUserLookup>();
        services.AddScoped<IConsentContext, IdentityConsentContext>();

        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResendVerificationHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<RevokeTokenHandler>();
        services.AddScoped<RevokeAllSessionsHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<ConfirmPasswordResetHandler>();
        services.AddScoped<GetProfileHandler>();
        services.AddScoped<UpdateProfileHandler>();

        services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();

        services.AddIdentityJwtAuthentication(configuration);
        services.AddIdentityPermissionPolicies();

        return services;
    }
}

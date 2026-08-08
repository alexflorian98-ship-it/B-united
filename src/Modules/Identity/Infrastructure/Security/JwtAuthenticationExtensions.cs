using System.Text;
using BUnited.Modules.Identity.Application.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BUnited.Modules.Identity.Infrastructure.Security;

public static class JwtAuthenticationExtensions
{
    /// <summary>RFC 7518 §3.2: an HS256 key SHOULD be at least as long as the hash output (256 bits).</summary>
    private const int MinimumSigningKeyBytes = 32;

    public static IServiceCollection AddIdentityJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
        {
            throw new InvalidOperationException(
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)} is not configured. " +
                "Set Jwt__SigningKey (see .env.example) before starting the Api host.");
        }

        if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)} is too short for HS256 " +
                $"(must be at least {MinimumSigningKeyBytes} bytes / 256 bits, per RFC 7518 §3.2). " +
                "Generate a strong random value, e.g.: " +
                "pwsh -c \"[Convert]::ToBase64String((New-Object byte[] 48 | %{[System.Security.Cryptography.RandomNumberGenerator]::Fill($_); $_}))\".");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Without this, the handler remaps standard JWT claim names (e.g. "sub") to
                // legacy WS-Federation URIs, so ClaimsPrincipal.FindFirstValue(sub) would fail
                // even though the token really has a "sub" claim.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        return services;
    }
}

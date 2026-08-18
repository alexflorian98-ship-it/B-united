using System.Text;
using BUnited.Modules.Identity.Application.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace BUnited.Modules.Identity.Infrastructure.Security;

public static class JwtAuthenticationExtensions
{
    /// <summary>RFC 7518 §3.2: an HS256 key SHOULD be at least as long as the hash output (256 bits).</summary>
    private const int MinimumSigningKeyBytes = 32;

    /// <summary>.env.example's documented example value — long enough to pass the length check
    /// below on its own, so it needs its own explicit rejection (security-gap-closure item #8:
    /// "placeholder secrets rejected at startup"). Because .env.example is committed to the repo,
    /// anyone can forge a validly-signed JWT for any user/permission if an operator ever deploys
    /// with this literal value still in place.</summary>
    private const string DocumentedPlaceholderSigningKey = "change-me-to-a-random-base64-value-at-least-32-bytes-long";

    public static IServiceCollection AddIdentityJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
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

        // Only enforced in Production: local/Development/demo deployments legitimately run with
        // the documented example value from .env.example, and this must never block them.
        if (environment?.IsProduction() == true && jwtOptions.SigningKey == DocumentedPlaceholderSigningKey)
        {
            throw new InvalidOperationException(
                $"Refusing to start in Production: {JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)} " +
                "is still the documented example value from .env.example. Generate a real random signing " +
                "key before deploying to Production.");
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

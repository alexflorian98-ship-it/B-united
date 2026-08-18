using Microsoft.AspNetCore.Builder;

namespace BUnited.BuildingBlocks.Security.Headers;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseBUnitedSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}

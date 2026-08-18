using Microsoft.AspNetCore.Http;

namespace BUnited.BuildingBlocks.Security.Headers;

/// <summary>
/// Attaches defensive response headers to every response — success, validation errors, 401/403,
/// 404, exception-handler responses, and rate-limit 429 responses alike. Registered via
/// <see cref="Response.OnStarting"/> rather than by writing the headers directly in
/// <see cref="InvokeAsync"/>: that callback fires exactly once, right before the first byte of
/// any response is sent, regardless of which downstream middleware (routing, MVC, the exception
/// handler, or the rate limiter) ultimately produces that response. Registering this middleware
/// first in the pipeline (see Program.cs) means the callback is queued before any other
/// middleware has a chance to short-circuit the pipeline.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Stops browsers from MIME-sniffing a response away from the declared Content-Type
            // (docs/E2E_AUDIT_RESULT.md, 2026-08-18: confirmed missing on an unknown-route 404).
            headers["X-Content-Type-Options"] = "nosniff";

            // This host serves only the JSON API (no HTML views outside Development's Swagger
            // UI), so it has no legitimate reason to be framed by another origin.
            headers["X-Frame-Options"] = "DENY";

            // Limits how much of the API's own URL leaks to third-party destinations reachable
            // from API responses (e.g. redirects, Location headers) without breaking same-origin
            // and same-site navigation the SPA relies on.
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // This API never uses camera/microphone/geolocation and serves no embeddable content;
            // deny every browser feature the platform does not use.
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // Confirmed via a real OWASP ZAP active scan (docs/security/DAST.md, 2026-08-18):
            // restricts which cross-origin contexts may embed/read this API's responses at the
            // browser level (Spectre-class side-channel mitigation) — narrower and newer than the
            // CORS policy, which already restricts cross-origin access separately.
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            // Same DAST pass flagged 404 responses lacking an explicit no-store directive. This
            // is a pure JSON API with nothing that benefits from HTTP caching (no static assets,
            // no cacheable business responses), so applying it unconditionally is safe and closes
            // the finding for every response, not just 404s.
            headers["Cache-Control"] = "no-store";

            return Task.CompletedTask;
        });

        return _next(context);
    }
}

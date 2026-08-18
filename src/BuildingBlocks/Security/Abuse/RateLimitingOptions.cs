namespace BUnited.BuildingBlocks.Security.Abuse;

/// <summary>
/// Configuration-bound rate limiting budgets. Defaults here are the production values and are
/// intentionally unchanged from the original hardcoded limits (100 req/min global, 5 req/min
/// auth) — appsettings.json does not override them, so Production keeps exactly this budget
/// unless a future explicit decision changes it. Only appsettings.Development.json raises the
/// auth budget, giving the local canonical Playwright run headroom above its 5-login floor.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public WindowOptions Global { get; set; } = new() { PermitLimit = 100, WindowSeconds = 60 };

    public WindowOptions Auth { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };

    public sealed class WindowOptions
    {
        public int PermitLimit { get; set; }

        public int WindowSeconds { get; set; }
    }
}

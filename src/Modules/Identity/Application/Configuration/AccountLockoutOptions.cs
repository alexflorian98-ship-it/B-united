namespace BUnited.Modules.Identity.Application.Configuration;

public sealed class AccountLockoutOptions
{
    public const string SectionName = "AccountLockout";

    public int MaxFailedAttempts { get; set; } = 5;

    public int LockoutDurationMinutes { get; set; } = 15;
}

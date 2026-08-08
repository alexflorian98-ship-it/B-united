namespace BUnited.Modules.Identity.Domain.Entities;

/// <summary>
/// Per-user preferences (§32, §64): timezone/language for localized formatting and
/// notification opt-in. One row per user (<see cref="UserId"/> is both PK and FK).
/// </summary>
public sealed class UserPreference
{
    /// <summary>Default per docs/PROMPT.md §62–64: "User timezone defaults to Europe/Bucharest, configurable per profile."</summary>
    public const string DefaultTimezone = "Europe/Bucharest";

    /// <summary>Romanian is the platform's default UI language (see CLAUDE.md).</summary>
    public const string DefaultLanguage = "ro";

    private UserPreference()
    {
    }

    public static UserPreference CreateDefault(Guid userId, string timezone = DefaultTimezone, string language = DefaultLanguage) =>
        new()
        {
            UserId = userId,
            Timezone = timezone,
            PreferredLanguage = language,
            EmailNotificationsEnabled = true,
        };

    public Guid UserId { get; private set; }

    public string Timezone { get; private set; } = DefaultTimezone;

    public string PreferredLanguage { get; private set; } = DefaultLanguage;

    public bool EmailNotificationsEnabled { get; private set; } = true;

    public void UpdateTimezone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
        {
            throw new ArgumentException("Timezone is required.", nameof(timezone));
        }

        Timezone = timezone;
    }

    public void UpdatePreferredLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            throw new ArgumentException("Language is required.", nameof(language));
        }

        PreferredLanguage = language;
    }

    public void SetEmailNotificationsEnabled(bool enabled) => EmailNotificationsEnabled = enabled;
}

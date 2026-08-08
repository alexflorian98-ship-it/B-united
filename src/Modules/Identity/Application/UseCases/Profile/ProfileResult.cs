namespace BUnited.Modules.Identity.Application.UseCases.Profile;

public sealed record ProfileResult(
    Guid UserId,
    string Email,
    string Timezone,
    string PreferredLanguage,
    bool EmailNotificationsEnabled);

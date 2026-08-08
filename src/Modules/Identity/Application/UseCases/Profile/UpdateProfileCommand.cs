namespace BUnited.Modules.Identity.Application.UseCases.Profile;

public sealed record UpdateProfileCommand(Guid UserId, string Timezone, string PreferredLanguage, bool EmailNotificationsEnabled);

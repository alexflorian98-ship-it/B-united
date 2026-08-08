namespace BUnited.Modules.Identity.Application.UseCases.Profile;

/// <summary>The client-bindable request body — deliberately has no <c>UserId</c> field so a
/// caller can never target another account's profile (mass-assignment/IDOR guard); the
/// controller always derives the target user from the authenticated principal.</summary>
public sealed record UpdateProfileRequest(string Timezone, string PreferredLanguage, bool EmailNotificationsEnabled);

namespace BUnited.Modules.Identity.Contracts;

/// <summary>
/// Read-only cross-module lookup for the email-notification opt-in (docs/PROMPT.md §32: "Security
/// and transactional notifications cannot be disabled. Marketing/community notifications may be
/// disabled.") — e.g. the Events reminder job (P5.09.a) must not email a user who disabled
/// notifications. Mirrors <see cref="IUserLookup"/>'s pattern: a narrow Contracts-mediated
/// projection, never a reference to Identity's Domain/Infrastructure.
/// </summary>
public interface INotificationPreferenceLookup
{
    Task<bool> AreEmailNotificationsEnabledAsync(Guid userId, CancellationToken cancellationToken);
}

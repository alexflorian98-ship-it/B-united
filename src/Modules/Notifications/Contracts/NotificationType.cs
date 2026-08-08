namespace BUnited.Modules.Notifications.Contracts;

/// <summary>
/// Notification types per docs/PROMPT.md §32. Only a subset has an actual call site wired yet
/// (see each module's use of <see cref="INotificationSender"/>) — the full enum is defined
/// up front because it is an authoritative, spec-fixed list, not a speculative extension point.
/// </summary>
public enum NotificationType
{
    EmailVerification,
    PasswordReset,
    Welcome,
    SubscriptionActivated,
    PaymentFailed,
    SubscriptionEnding,
    QuestionnaireSubmitted,
    GuidancePublished,
    EventRegistrationConfirmed,
    EventReminder,
    ChatPinnedMessage,
}

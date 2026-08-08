namespace BUnited.Modules.Notifications.Contracts;

/// <summary>
/// Shared notification-sending abstraction (docs/PROMPT.md §32 — "small channel abstraction").
/// Other modules depend on this interface only, never on Notifications' Domain/Infrastructure,
/// per the module-boundary rule. <paramref name="templateData"/> MUST NEVER contain sensitive
/// payloads (questionnaire answers, guidance text, tokens, card data — CLAUDE.md's logging rule
/// applies here too, since the current V1 implementation logs instead of sending real email);
/// pass identifiers (submission id, plan name) for the template to look up, not raw content.
/// </summary>
public interface INotificationSender
{
    Task SendAsync(
        NotificationType type,
        string recipientEmail,
        IReadOnlyDictionary<string, string> templateData,
        CancellationToken cancellationToken);
}

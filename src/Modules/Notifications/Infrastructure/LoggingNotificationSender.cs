using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Notifications.Contracts;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Notifications.Infrastructure;

/// <summary>
/// V1 implementation of <see cref="INotificationSender"/>: structured-logs that a notification
/// would be sent, instead of actually sending email. There is no real email provider configured
/// in this environment (mirrors Identity's own <c>LoggingIdentityEmailSender</c>, built in
/// Phase 1 for the same reason). Never logs <paramref name="templateData"/> values, only that an
/// event of a given type occurred and to which recipient — consistent with never logging
/// sensitive payloads (CLAUDE.md). Implements <see cref="IDemoOnlyAdapter"/> so the P3.32
/// production-safety startup check refuses to boot in <c>Production</c> with no real sender wired.
/// </summary>
public sealed class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender, IDemoOnlyAdapter
{
    public Task SendAsync(
        NotificationType type,
        string recipientEmail,
        IReadOnlyDictionary<string, string> templateData,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "notifications.would_send: {NotificationType} would be sent to {RecipientEmail}",
            type,
            recipientEmail);
        return Task.CompletedTask;
    }
}

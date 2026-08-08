using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Infrastructure.Notifications;

/// <summary>
/// Temporary log-only stand-in for <see cref="IIdentityEmailSender"/>: the Notifications
/// module (its own module per docs/ARCHITECTURE.md — "Notifications: Email notification
/// sending abstraction") now exists (Phase 4) but Identity's auth-critical emails
/// (verification/reset) haven't been migrated onto it in this pass — that's a separate,
/// deliberate follow-up, not an oversight. Never logs the raw token (§6) — only that a
/// notification event occurred, for observability during development. Implements
/// <see cref="IDemoOnlyAdapter"/> so the P3.32 production-safety startup check refuses to boot
/// in <c>Production</c> with no real sender wired.
/// </summary>
public sealed class LoggingIdentityEmailSender(ILogger<LoggingIdentityEmailSender> logger) : IIdentityEmailSender, IDemoOnlyAdapter
{
    public Task SendEmailVerificationAsync(string email, string rawVerificationToken, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "identity.email_verification_requested: verification email would be sent to {Email}",
            email);
        return Task.CompletedTask;
    }

    public Task SendWelcomeAsync(string email, CancellationToken cancellationToken)
    {
        logger.LogInformation("identity.welcome_email_requested: welcome email would be sent to {Email}", email);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string rawResetToken, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "identity.password_reset_requested: password reset email would be sent to {Email}",
            email);
        return Task.CompletedTask;
    }
}

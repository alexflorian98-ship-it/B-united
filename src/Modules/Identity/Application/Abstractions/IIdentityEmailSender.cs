namespace BUnited.Modules.Identity.Application.Abstractions;

/// <summary>
/// Sends Identity-related transactional emails. Temporary log-only stub implementation lives
/// in Identity.Infrastructure until the Notifications module (its own module, per
/// docs/ARCHITECTURE.md) provides a real email provider — see the note on
/// <c>LoggingIdentityEmailSender</c>. Never pass the raw token to a logger.
/// </summary>
public interface IIdentityEmailSender
{
    Task SendEmailVerificationAsync(string email, string rawVerificationToken, CancellationToken cancellationToken);

    Task SendWelcomeAsync(string email, CancellationToken cancellationToken);

    Task SendPasswordResetAsync(string email, string rawResetToken, CancellationToken cancellationToken);
}

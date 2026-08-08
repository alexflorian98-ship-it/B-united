using BUnited.Modules.Identity.Application.Abstractions;

namespace BUnited.Modules.Identity.Tests.TestSupport;

internal sealed class CapturingEmailSender : IIdentityEmailSender
{
    public string? LastVerificationEmail { get; private set; }

    public string? LastVerificationToken { get; private set; }

    public string? LastResetEmail { get; private set; }

    public string? LastResetToken { get; private set; }

    public bool WelcomeSent { get; private set; }

    public Task SendEmailVerificationAsync(string email, string rawVerificationToken, CancellationToken cancellationToken)
    {
        LastVerificationEmail = email;
        LastVerificationToken = rawVerificationToken;
        return Task.CompletedTask;
    }

    public Task SendWelcomeAsync(string email, CancellationToken cancellationToken)
    {
        WelcomeSent = true;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string rawResetToken, CancellationToken cancellationToken)
    {
        LastResetEmail = email;
        LastResetToken = rawResetToken;
        return Task.CompletedTask;
    }
}

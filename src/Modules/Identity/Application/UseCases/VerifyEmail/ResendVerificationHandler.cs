using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.VerifyEmail;

/// <summary>Issues a fresh verification token when the original link expired, was already
/// used, or was lost — the frontend's "resend guidance" state for email verification
/// (P1.39.a). Deliberately behaves identically regardless of whether the account exists or is
/// already verified, for the same non-enumeration reason as <c>RequestPasswordResetHandler</c>.</summary>
public sealed class ResendVerificationHandler(
    DbContext dbContext,
    ISecureTokenGenerator tokenGenerator,
    IIdentityEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<ResendVerificationHandler> logger)
{
    public async Task HandleAsync(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = User.Normalize(command.Email);

        var user = await dbContext.Set<User>()
            .SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || user.IsEmailVerified)
        {
            logger.LogInformation("identity.verification_resend_requested_noop");
            return;
        }

        var (rawToken, tokenHash) = tokenGenerator.Generate();
        var verificationToken = EmailVerificationToken.Issue(user.Id, tokenHash, utcNow, utcNow.AddHours(24));
        dbContext.Set<EmailVerificationToken>().Add(verificationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await emailSender.SendEmailVerificationAsync(user.Email, rawToken, cancellationToken);

        logger.LogInformation("identity.verification_resend: UserId {UserId}", user.Id);
    }
}

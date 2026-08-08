using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.VerifyEmail;

public sealed class VerifyEmailHandler(
    DbContext dbContext,
    ISecureTokenGenerator tokenGenerator,
    IIdentityEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<VerifyEmailHandler> logger)
{
    public async Task HandleAsync(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = tokenGenerator.Hash(command.Token);

        var token = await dbContext.Set<EmailVerificationToken>()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is null || !token.IsValid(utcNow))
        {
            throw new BusinessRuleAppException(
                "EMAIL_VERIFICATION_TOKEN_INVALID",
                "errors.emailVerificationTokenInvalid",
                "The email verification token is invalid, expired, or already used.");
        }

        var user = await dbContext.Set<User>().SingleAsync(u => u.Id == token.UserId, cancellationToken);

        token.MarkUsed(utcNow);

        var wasAlreadyVerified = user.IsEmailVerified;
        user.MarkEmailVerified(utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!wasAlreadyVerified)
        {
            await emailSender.SendWelcomeAsync(user.Email, cancellationToken);
        }

        logger.LogInformation("identity.email_verified: UserId {UserId}", user.Id);
    }
}

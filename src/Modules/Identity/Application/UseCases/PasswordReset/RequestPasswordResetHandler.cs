using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.PasswordReset;

public sealed class RequestPasswordResetHandler(
    DbContext dbContext,
    ISecureTokenGenerator tokenGenerator,
    IIdentityEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<RequestPasswordResetHandler> logger)
{
    public async Task HandleAsync(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = User.Normalize(command.Email);

        var user = await dbContext.Set<User>()
            .SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        // Always behaves the same way regardless of whether the account exists (§22.a) — never
        // reveal account existence through the response shape.
        if (user is null)
        {
            logger.LogInformation("identity.password_reset_requested_unknown_email");
            return;
        }

        var (rawToken, tokenHash) = tokenGenerator.Generate();
        var resetToken = PasswordResetToken.Issue(user.Id, tokenHash, utcNow, utcNow.AddHours(1));
        dbContext.Set<PasswordResetToken>().Add(resetToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await emailSender.SendPasswordResetAsync(user.Email, rawToken, cancellationToken);

        logger.LogInformation("identity.password_reset_requested: UserId {UserId}", user.Id);
    }
}

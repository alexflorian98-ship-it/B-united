using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.PasswordReset;

public sealed class ConfirmPasswordResetHandler(
    DbContext dbContext,
    IPasswordHasher passwordHasher,
    ISecureTokenGenerator tokenGenerator,
    TimeProvider timeProvider,
    IAuditLogger auditLogger,
    ILogger<ConfirmPasswordResetHandler> logger)
{
    public async Task HandleAsync(ConfirmPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = tokenGenerator.Hash(command.Token);

        var token = await dbContext.Set<PasswordResetToken>()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is null || !token.IsValid(utcNow))
        {
            throw new BusinessRuleAppException(
                "PASSWORD_RESET_TOKEN_INVALID",
                "errors.passwordResetTokenInvalid",
                "The password reset token is invalid, expired, or already used.");
        }

        var user = await dbContext.Set<User>().SingleAsync(u => u.Id == token.UserId, cancellationToken);

        token.MarkUsed(utcNow);
        user.ChangePasswordHash(passwordHasher.Hash(command.NewPassword));
        user.ResetFailedLoginAttempts();

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("identity.password_reset: UserId {UserId}", user.Id);
        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.UserPasswordReset, actorUserId: user.Id, entityType: "User", entityId: user.Id.ToString()),
            cancellationToken);
    }
}

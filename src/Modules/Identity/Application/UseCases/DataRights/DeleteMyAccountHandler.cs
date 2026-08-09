using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.DataRights;

public sealed record DeleteMyAccountCommand(Guid UserId, string CurrentPassword);

/// <summary>Orchestrates self-service, irreversible account deletion (docs/PROMPT.md §66,
/// docs/DATA_RETENTION_POLICY.md). Requires the caller's current password as a confirmation step
/// (docs/DEVELOPMENT_INSTRUCTIONS.md §2 — this must never be triggerable by a stray click or CSRF
/// alone), mirroring how <c>ConfirmPasswordResetHandler</c> treats a destructive credential
/// action.
///
/// Fans out to every registered <see cref="IUserDataEraser"/> — one per module whose data must be
/// hard-deleted/anonymized/soft-canceled per the retention policy — then anonymizes the
/// <c>User</c> row itself and revokes every refresh token, all staged on the same shared
/// <c>DbContext</c> and committed in a single <c>SaveChangesAsync</c> call so the whole operation
/// is one atomic transaction. Billing is deliberately not represented by any eraser: its records
/// are retained per policy and untouched by this handler.</summary>
public sealed class DeleteMyAccountHandler(
    DbContext dbContext,
    IEnumerable<IUserDataEraser> erasers,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    IAuditLogger auditLogger,
    ILogger<DeleteMyAccountHandler> logger)
{
    public async Task HandleAsync(DeleteMyAccountCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            throw new BusinessRuleAppException(
                "ACCOUNT_DELETION_PASSWORD_INVALID",
                "errors.accountDeletionPasswordInvalid",
                "The current password is required.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var user = await dbContext.Set<User>().SingleAsync(u => u.Id == command.UserId, cancellationToken);

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            logger.LogWarning("identity.account_deletion_rejected: UserId {UserId} wrong password", user.Id);
            throw new BusinessRuleAppException(
                "ACCOUNT_DELETION_PASSWORD_INVALID",
                "errors.accountDeletionPasswordInvalid",
                "The current password is incorrect.");
        }

        // Fan out to every other module's own erasure/anonymization logic first (all staged on
        // this same DbContext, not yet committed).
        foreach (var eraser in erasers)
        {
            await eraser.EraseAsync(command.UserId, cancellationToken);
        }

        // Identity's own disposable data: revoke every session and drop tokens/preferences with
        // no retention value. UserConsent is deliberately left untouched (compliance record).
        var refreshTokens = await dbContext.Set<RefreshToken>()
            .Where(t => t.UserId == command.UserId)
            .ToListAsync(cancellationToken);
        dbContext.Set<RefreshToken>().RemoveRange(refreshTokens);

        var emailVerificationTokens = await dbContext.Set<EmailVerificationToken>()
            .Where(t => t.UserId == command.UserId)
            .ToListAsync(cancellationToken);
        dbContext.Set<EmailVerificationToken>().RemoveRange(emailVerificationTokens);

        var passwordResetTokens = await dbContext.Set<PasswordResetToken>()
            .Where(t => t.UserId == command.UserId)
            .ToListAsync(cancellationToken);
        dbContext.Set<PasswordResetToken>().RemoveRange(passwordResetTokens);

        var preference = await dbContext.Set<UserPreference>()
            .SingleOrDefaultAsync(p => p.UserId == command.UserId, cancellationToken);
        if (preference is not null)
        {
            dbContext.Set<UserPreference>().Remove(preference);
        }

        var anonymizedPasswordHash = passwordHasher.Hash($"{Guid.NewGuid():N}{Guid.NewGuid():N}");
        user.AnonymizeForDeletion(utcNow, anonymizedPasswordHash);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("identity.account_deleted: UserId {UserId}", user.Id);
        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.UserAccountDeleted, actorUserId: user.Id, entityType: "User", entityId: user.Id.ToString()),
            cancellationToken);
    }
}

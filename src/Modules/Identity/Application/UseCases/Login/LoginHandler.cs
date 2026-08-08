using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Application.UseCases.Common;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Identity.Application.UseCases.Login;

public sealed class LoginHandler(
    DbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    ISecureTokenGenerator tokenGenerator,
    IOptions<JwtOptions> jwtOptions,
    IOptions<AccountLockoutOptions> lockoutOptions,
    TimeProvider timeProvider,
    IAuditLogger auditLogger,
    ILogger<LoginHandler> logger)
{
    public async Task<TokenPairResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = User.Normalize(command.Email);

        var user = await dbContext.Set<User>()
            .SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("identity.failed_login: unknown email");
            await AuditFailedLoginAsync(null, "unknown_email", cancellationToken);
            throw InvalidCredentials();
        }

        if (user.IsLockedOut(utcNow))
        {
            logger.LogWarning("identity.failed_login: UserId {UserId} is locked out", user.Id);
            await AuditFailedLoginAsync(user.Id, "locked_out", cancellationToken);
            throw new BusinessRuleAppException(
                "ACCOUNT_LOCKED",
                "errors.accountLocked",
                "The account is temporarily locked due to too many failed login attempts.");
        }

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            var lockout = lockoutOptions.Value;
            user.RegisterFailedLoginAttempt(
                utcNow,
                lockout.MaxFailedAttempts,
                TimeSpan.FromMinutes(lockout.LockoutDurationMinutes));
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogWarning("identity.failed_login: UserId {UserId} wrong password", user.Id);
            await AuditFailedLoginAsync(user.Id, "wrong_password", cancellationToken);
            throw InvalidCredentials();
        }

        if (!user.IsEmailVerified)
        {
            await AuditFailedLoginAsync(user.Id, "email_not_verified", cancellationToken);
            throw new BusinessRuleAppException(
                "EMAIL_NOT_VERIFIED",
                "errors.emailNotVerified",
                "The account email address has not been verified yet.");
        }

        user.ResetFailedLoginAttempts();

        var permissionKeys = await LoadPermissionKeysAsync(dbContext, user.Id, cancellationToken);
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, permissionKeys);

        var (rawRefreshToken, refreshTokenHash) = tokenGenerator.Generate();
        var refreshTokenExpiresAtUtc = utcNow.AddDays(jwtOptions.Value.RefreshTokenLifetimeDays);
        var refreshToken = RefreshToken.IssueNew(user.Id, refreshTokenHash, utcNow, refreshTokenExpiresAtUtc);
        dbContext.Set<RefreshToken>().Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("identity.login: UserId {UserId}", user.Id);
        await auditLogger.LogAsync(
            AuditEntry.Create(AuditActions.UserLogin, actorUserId: user.Id, entityType: "User", entityId: user.Id.ToString()),
            cancellationToken);

        return new TokenPairResult(accessToken.Token, accessToken.ExpiresAtUtc, rawRefreshToken, refreshTokenExpiresAtUtc);
    }

    private Task AuditFailedLoginAsync(Guid? userId, string reason, CancellationToken cancellationToken) =>
        auditLogger.LogAsync(
            AuditEntry.Create(
                AuditActions.UserFailedLogin,
                actorUserId: userId,
                entityType: userId is null ? null : "User",
                entityId: userId?.ToString(),
                metadata: new Dictionary<string, string> { ["reason"] = reason }),
            cancellationToken);

    internal static Task<List<string>> LoadPermissionKeysAsync(DbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToListAsync(cancellationToken);

    private static BusinessRuleAppException InvalidCredentials() =>
        new("INVALID_CREDENTIALS", "errors.invalidCredentials", "The email or password is incorrect.");
}

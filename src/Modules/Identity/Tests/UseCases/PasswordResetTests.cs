using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Application.UseCases.PasswordReset;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class PasswordResetTests
{
    [Fact]
    public async Task Request_issues_a_token_and_emails_it_when_the_account_exists()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("ada@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailSender = new CapturingEmailSender();
        var handler = new RequestPasswordResetHandler(
            context,
            new SecureTokenGenerator(),
            emailSender,
            TimeProvider.System,
            NullLogger<RequestPasswordResetHandler>.Instance);

        await handler.HandleAsync(new RequestPasswordResetCommand("ada@example.com"), CancellationToken.None);

        Assert.NotNull(emailSender.LastResetToken);
        Assert.Single(context.PasswordResetTokens.Local);
    }

    [Fact]
    public async Task Request_does_not_throw_or_send_email_for_an_unknown_account()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var emailSender = new CapturingEmailSender();
        var handler = new RequestPasswordResetHandler(
            context,
            new SecureTokenGenerator(),
            emailSender,
            TimeProvider.System,
            NullLogger<RequestPasswordResetHandler>.Instance);

        // Must not throw — always behaves the same regardless of account existence.
        await handler.HandleAsync(new RequestPasswordResetCommand("nobody@example.com"), CancellationToken.None);

        Assert.Null(emailSender.LastResetToken);
    }

    [Fact]
    public async Task Confirm_changes_the_password_and_clears_lockout()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var passwordHasher = new PasswordHasher();
        var user = User.Register("ada@example.com", passwordHasher.Hash("OldPassword123"));
        user.RegisterFailedLoginAttempt(DateTime.UtcNow, maxAttempts: 1, lockoutDuration: TimeSpan.FromMinutes(15));
        context.Users.Add(user);

        var tokenGenerator = new SecureTokenGenerator();
        var (rawToken, tokenHash) = tokenGenerator.Generate();
        var now = DateTime.UtcNow;
        context.PasswordResetTokens.Add(PasswordResetToken.Issue(user.Id, tokenHash, now, now.AddHours(1)));
        await context.SaveChangesAsync();

        var auditLogger = new RecordingAuditLogger();
        var handler = new ConfirmPasswordResetHandler(
            context,
            passwordHasher,
            tokenGenerator,
            TimeProvider.System,
            auditLogger,
            NullLogger<ConfirmPasswordResetHandler>.Instance);

        await handler.HandleAsync(new ConfirmPasswordResetCommand(rawToken, "NewPassword456"), CancellationToken.None);

        var updatedUser = await context.Users.SingleAsync(u => u.Id == user.Id);
        Assert.True(passwordHasher.Verify("NewPassword456", updatedUser.PasswordHash));
        Assert.False(updatedUser.IsLockedOut(DateTime.UtcNow));

        var auditEntry = Assert.Single(auditLogger.Entries);
        Assert.Equal(AuditActions.UserPasswordReset, auditEntry.Action);
        Assert.Equal(user.Id, auditEntry.ActorUserId);
    }

    [Fact]
    public async Task Confirm_rejects_an_already_used_token()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var passwordHasher = new PasswordHasher();
        var user = User.Register("ada@example.com", passwordHasher.Hash("OldPassword123"));
        context.Users.Add(user);

        var tokenGenerator = new SecureTokenGenerator();
        var (rawToken, tokenHash) = tokenGenerator.Generate();
        var now = DateTime.UtcNow;
        context.PasswordResetTokens.Add(PasswordResetToken.Issue(user.Id, tokenHash, now, now.AddHours(1)));
        await context.SaveChangesAsync();

        var handler = new ConfirmPasswordResetHandler(
            context,
            passwordHasher,
            tokenGenerator,
            TimeProvider.System,
            new RecordingAuditLogger(),
            NullLogger<ConfirmPasswordResetHandler>.Instance);

        await handler.HandleAsync(new ConfirmPasswordResetCommand(rawToken, "NewPassword456"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new ConfirmPasswordResetCommand(rawToken, "AnotherPassword789"), CancellationToken.None));

        Assert.Equal("PASSWORD_RESET_TOKEN_INVALID", exception.Code);
    }

    [Fact]
    public async Task Confirm_rejects_an_expired_token()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var passwordHasher = new PasswordHasher();
        var user = User.Register("ada@example.com", passwordHasher.Hash("OldPassword123"));
        context.Users.Add(user);

        var tokenGenerator = new SecureTokenGenerator();
        var (rawToken, tokenHash) = tokenGenerator.Generate();
        var now = DateTime.UtcNow;
        context.PasswordResetTokens.Add(PasswordResetToken.Issue(user.Id, tokenHash, now.AddHours(-2), now.AddHours(-1)));
        await context.SaveChangesAsync();

        var handler = new ConfirmPasswordResetHandler(
            context,
            passwordHasher,
            tokenGenerator,
            TimeProvider.System,
            new RecordingAuditLogger(),
            NullLogger<ConfirmPasswordResetHandler>.Instance);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new ConfirmPasswordResetCommand(rawToken, "NewPassword456"), CancellationToken.None));

        Assert.Equal("PASSWORD_RESET_TOKEN_INVALID", exception.Code);
    }
}

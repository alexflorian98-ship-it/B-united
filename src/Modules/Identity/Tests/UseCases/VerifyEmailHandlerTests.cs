using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Identity.Application.UseCases.VerifyEmail;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class VerifyEmailHandlerTests
{
    private static async Task<(TestDbContext Context, Microsoft.Data.Sqlite.SqliteConnection Connection, User User, string RawToken)> SeedUnverifiedUserAsync(
        DateTime? expiresAtUtc = null)
    {
        var (connection, context) = TestDbContextFactory.Create();
        var user = User.Register("ada@example.com", "hash");
        context.Users.Add(user);

        var tokenGenerator = new SecureTokenGenerator();
        var (rawToken, tokenHash) = tokenGenerator.Generate();
        var now = DateTime.UtcNow;
        context.EmailVerificationTokens.Add(
            EmailVerificationToken.Issue(user.Id, tokenHash, now, expiresAtUtc ?? now.AddHours(24)));

        await context.SaveChangesAsync();

        return (context, connection, user, rawToken);
    }

    [Fact]
    public async Task Valid_token_verifies_the_user_and_sends_a_welcome_email()
    {
        var (context, connection, user, rawToken) = await SeedUnverifiedUserAsync();
        using var _ = connection;
        using var __ = context;

        var emailSender = new CapturingEmailSender();
        var handler = new VerifyEmailHandler(
            context,
            new SecureTokenGenerator(),
            emailSender,
            TimeProvider.System,
            NullLogger<VerifyEmailHandler>.Instance);

        await handler.HandleAsync(new VerifyEmailCommand(rawToken), CancellationToken.None);

        var updatedUser = await context.Users.SingleAsync(u => u.Id == user.Id);
        Assert.True(updatedUser.IsEmailVerified);
        Assert.True(emailSender.WelcomeSent);
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        var (context, connection, _, rawToken) = await SeedUnverifiedUserAsync(expiresAtUtc: DateTime.UtcNow.AddHours(-1));
        using var _ = connection;
        using var __ = context;

        var handler = new VerifyEmailHandler(
            context,
            new SecureTokenGenerator(),
            new CapturingEmailSender(),
            TimeProvider.System,
            NullLogger<VerifyEmailHandler>.Instance);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new VerifyEmailCommand(rawToken), CancellationToken.None));

        Assert.Equal("EMAIL_VERIFICATION_TOKEN_INVALID", exception.Code);
    }

    [Fact]
    public async Task Reusing_an_already_used_token_is_rejected()
    {
        var (context, connection, _, rawToken) = await SeedUnverifiedUserAsync();
        using var _ = connection;
        using var __ = context;

        var handler = new VerifyEmailHandler(
            context,
            new SecureTokenGenerator(),
            new CapturingEmailSender(),
            TimeProvider.System,
            NullLogger<VerifyEmailHandler>.Instance);

        await handler.HandleAsync(new VerifyEmailCommand(rawToken), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new VerifyEmailCommand(rawToken), CancellationToken.None));

        Assert.Equal("EMAIL_VERIFICATION_TOKEN_INVALID", exception.Code);
    }
}

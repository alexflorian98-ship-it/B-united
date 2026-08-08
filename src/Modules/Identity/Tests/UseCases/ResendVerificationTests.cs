using BUnited.Modules.Identity.Application.UseCases.VerifyEmail;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class ResendVerificationTests
{
    [Fact]
    public async Task Issues_a_new_token_and_emails_it_for_an_unverified_account()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("ada@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailSender = new CapturingEmailSender();
        var handler = new ResendVerificationHandler(
            context,
            new SecureTokenGenerator(),
            emailSender,
            TimeProvider.System,
            NullLogger<ResendVerificationHandler>.Instance);

        await handler.HandleAsync(new ResendVerificationCommand("ada@example.com"), CancellationToken.None);

        Assert.NotNull(emailSender.LastVerificationToken);
        Assert.Equal(1, await context.EmailVerificationTokens.CountAsync(t => t.UserId == user.Id));
    }

    [Fact]
    public async Task A_second_resend_issues_another_token_alongside_the_first()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("ada@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new ResendVerificationHandler(
            context,
            new SecureTokenGenerator(),
            new CapturingEmailSender(),
            TimeProvider.System,
            NullLogger<ResendVerificationHandler>.Instance);

        await handler.HandleAsync(new ResendVerificationCommand("ada@example.com"), CancellationToken.None);
        await handler.HandleAsync(new ResendVerificationCommand("ada@example.com"), CancellationToken.None);

        Assert.Equal(2, await context.EmailVerificationTokens.CountAsync(t => t.UserId == user.Id));
    }

    [Fact]
    public async Task Does_not_throw_or_send_email_for_an_unknown_account()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var emailSender = new CapturingEmailSender();
        var handler = new ResendVerificationHandler(
            context,
            new SecureTokenGenerator(),
            emailSender,
            TimeProvider.System,
            NullLogger<ResendVerificationHandler>.Instance);

        await handler.HandleAsync(new ResendVerificationCommand("nobody@example.com"), CancellationToken.None);

        Assert.Null(emailSender.LastVerificationToken);
    }

    [Fact]
    public async Task Does_not_send_an_email_for_an_already_verified_account()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("ada@example.com", "hash");
        user.MarkEmailVerified(DateTime.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailSender = new CapturingEmailSender();
        var handler = new ResendVerificationHandler(
            context,
            new SecureTokenGenerator(),
            emailSender,
            TimeProvider.System,
            NullLogger<ResendVerificationHandler>.Instance);

        await handler.HandleAsync(new ResendVerificationCommand("ada@example.com"), CancellationToken.None);

        Assert.Null(emailSender.LastVerificationToken);
    }
}

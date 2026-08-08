using BUnited.Modules.Identity.Application.UseCases.Register;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task Register_creates_user_with_client_role_and_issues_verification_token()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        context.Roles.Add(new Role(WellKnownRoles.ClientId, WellKnownRoles.Client));
        await context.SaveChangesAsync();

        var emailSender = new CapturingEmailSender();
        var handler = new RegisterUserHandler(
            context,
            new PasswordHasher(),
            new SecureTokenGenerator(),
            emailSender,
            TimeProvider.System,
            NullLogger<RegisterUserHandler>.Instance);

        var result = await handler.HandleAsync(new RegisterUserCommand("ada@example.com", "StrongPass123"), CancellationToken.None);

        var user = await context.Users.SingleAsync(u => u.Id == result.UserId);
        Assert.Equal("ada@example.com", user.Email);
        Assert.False(user.IsEmailVerified);
        Assert.Contains(user.UserRoles, ur => ur.RoleId == WellKnownRoles.ClientId);

        var preference = await context.UserPreferences.SingleAsync(p => p.UserId == user.Id);
        Assert.Equal("Europe/Bucharest", preference.Timezone);
        Assert.Equal("ro", preference.PreferredLanguage);

        var verificationToken = await context.EmailVerificationTokens.SingleAsync(t => t.UserId == user.Id);
        Assert.True(verificationToken.IsValid(DateTime.UtcNow));

        Assert.Equal("ada@example.com", emailSender.LastVerificationEmail);
        Assert.NotNull(emailSender.LastVerificationToken);
    }

    [Fact]
    public async Task Register_hashes_the_password_rather_than_storing_it_raw()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        context.Roles.Add(new Role(WellKnownRoles.ClientId, WellKnownRoles.Client));
        await context.SaveChangesAsync();

        var passwordHasher = new PasswordHasher();
        var handler = new RegisterUserHandler(
            context,
            passwordHasher,
            new SecureTokenGenerator(),
            new CapturingEmailSender(),
            TimeProvider.System,
            NullLogger<RegisterUserHandler>.Instance);

        var result = await handler.HandleAsync(new RegisterUserCommand("ada@example.com", "StrongPass123"), CancellationToken.None);

        var user = await context.Users.SingleAsync(u => u.Id == result.UserId);
        Assert.NotEqual("StrongPass123", user.PasswordHash);
        Assert.True(passwordHasher.Verify("StrongPass123", user.PasswordHash));
    }
}

using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Application.UseCases.Login;
using BUnited.Modules.Identity.Application.UseCases.Refresh;
using BUnited.Modules.Identity.Application.UseCases.Register;
using BUnited.Modules.Identity.Application.UseCases.Revoke;
using BUnited.Modules.Identity.Application.UseCases.VerifyEmail;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Identity.Tests.UseCases;

/// <summary>
/// Multi-step integration tests chaining the real handlers together (register → verify → login
/// → refresh → logout), exercising the same objects a single request would use rather than
/// re-testing each handler in isolation (that's covered by the per-handler test classes already).
/// </summary>
public sealed class AuthFlowTests
{
    private static readonly IOptions<JwtOptions> JwtOptions = Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "this-is-a-test-signing-key-that-is-long-enough-1234567890",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 30,
    });

    private static readonly IOptions<AccountLockoutOptions> LockoutOptions = Options.Create(new AccountLockoutOptions
    {
        MaxFailedAttempts = 5,
        LockoutDurationMinutes = 15,
    });

    [Fact]
    public async Task Register_verify_login_refresh_and_logout_flow_succeeds_end_to_end()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        context.Roles.Add(new Role(WellKnownRoles.ClientId, WellKnownRoles.Client));
        await context.SaveChangesAsync();

        var passwordHasher = new PasswordHasher();
        var tokenGenerator = new SecureTokenGenerator();
        var jwtTokenGenerator = new JwtTokenGenerator(JwtOptions);

        // 1. Register.
        var registerEmailSender = new CapturingEmailSender();
        var registerHandler = new RegisterUserHandler(
            context, passwordHasher, tokenGenerator, registerEmailSender, TimeProvider.System, NullLogger<RegisterUserHandler>.Instance);
        var registerResult = await registerHandler.HandleAsync(
            new RegisterUserCommand("flow@example.com", "StrongPass123"), CancellationToken.None);

        var registeredUser = await context.Users.SingleAsync(u => u.Id == registerResult.UserId);
        Assert.False(registeredUser.IsEmailVerified);
        Assert.NotNull(registerEmailSender.LastVerificationToken);

        // 2. Verify email using the real token the registration handler issued.
        var verifyHandler = new VerifyEmailHandler(
            context, tokenGenerator, new CapturingEmailSender(), TimeProvider.System, NullLogger<VerifyEmailHandler>.Instance);
        await verifyHandler.HandleAsync(new VerifyEmailCommand(registerEmailSender.LastVerificationToken!), CancellationToken.None);

        var verifiedUser = await context.Users.SingleAsync(u => u.Id == registerResult.UserId);
        Assert.True(verifiedUser.IsEmailVerified);

        // 3. Login.
        var loginAuditLogger = new RecordingAuditLogger();
        var loginHandler = new LoginHandler(
            context,
            passwordHasher,
            jwtTokenGenerator,
            tokenGenerator,
            JwtOptions,
            LockoutOptions,
            TimeProvider.System,
            loginAuditLogger,
            NullLogger<LoginHandler>.Instance);
        var loginResult = await loginHandler.HandleAsync(
            new LoginCommand("flow@example.com", "StrongPass123"), CancellationToken.None);

        Assert.NotEmpty(loginResult.AccessToken);
        Assert.NotEmpty(loginResult.RefreshToken);
        Assert.Contains(loginAuditLogger.Entries, e => e.Action == AuditActions.UserLogin);

        // 4. Refresh.
        var refreshHandler = new RefreshTokenHandler(
            context, jwtTokenGenerator, tokenGenerator, JwtOptions, TimeProvider.System, NullLogger<RefreshTokenHandler>.Instance);
        var refreshResult = await refreshHandler.HandleAsync(
            new RefreshTokenCommand(loginResult.RefreshToken), CancellationToken.None);

        Assert.NotEqual(loginResult.RefreshToken, refreshResult.RefreshToken);

        // The original login refresh token is now rotated away and can no longer be used.
        await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => refreshHandler.HandleAsync(new RefreshTokenCommand(loginResult.RefreshToken), CancellationToken.None));

        // 5. Logout (revoke the current, rotated refresh token).
        var revokeHandler = new RevokeTokenHandler(context, tokenGenerator, TimeProvider.System, NullLogger<RevokeTokenHandler>.Instance);
        await revokeHandler.HandleAsync(new RevokeTokenCommand(refreshResult.RefreshToken), CancellationToken.None);

        // Logged out: the just-revoked token can no longer refresh a session.
        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => refreshHandler.HandleAsync(new RefreshTokenCommand(refreshResult.RefreshToken), CancellationToken.None));
        Assert.Equal("REFRESH_TOKEN_INVALID", exception.Code);
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("expired-refresh@example.com", "hash");
        user.MarkEmailVerified(DateTime.UtcNow);
        context.Users.Add(user);

        var tokenGenerator = new SecureTokenGenerator();
        var (rawToken, tokenHash) = tokenGenerator.Generate();
        var now = DateTime.UtcNow;
        context.RefreshTokens.Add(RefreshToken.IssueNew(user.Id, tokenHash, now.AddDays(-31), now.AddDays(-1)));
        await context.SaveChangesAsync();

        var handler = new RefreshTokenHandler(
            context,
            new JwtTokenGenerator(JwtOptions),
            tokenGenerator,
            JwtOptions,
            TimeProvider.System,
            NullLogger<RefreshTokenHandler>.Instance);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new RefreshTokenCommand(rawToken), CancellationToken.None));

        Assert.Equal("REFRESH_TOKEN_INVALID", exception.Code);
    }

    [Fact]
    public async Task A_revoked_token_cannot_be_used_to_refresh()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("revoked-refresh@example.com", "hash");
        user.MarkEmailVerified(DateTime.UtcNow);
        context.Users.Add(user);

        var tokenGenerator = new SecureTokenGenerator();
        var (rawToken, tokenHash) = tokenGenerator.Generate();
        context.RefreshTokens.Add(RefreshToken.IssueNew(user.Id, tokenHash, DateTime.UtcNow, DateTime.UtcNow.AddDays(30)));
        await context.SaveChangesAsync();

        var revokeHandler = new RevokeTokenHandler(context, tokenGenerator, TimeProvider.System, NullLogger<RevokeTokenHandler>.Instance);
        await revokeHandler.HandleAsync(new RevokeTokenCommand(rawToken), CancellationToken.None);

        var refreshHandler = new RefreshTokenHandler(
            context,
            new JwtTokenGenerator(JwtOptions),
            tokenGenerator,
            JwtOptions,
            TimeProvider.System,
            NullLogger<RefreshTokenHandler>.Instance);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => refreshHandler.HandleAsync(new RefreshTokenCommand(rawToken), CancellationToken.None));

        Assert.Equal("REFRESH_TOKEN_INVALID", exception.Code);
    }
}

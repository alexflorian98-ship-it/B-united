using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Application.UseCases.Login;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class LoginHandlerTests
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

    private static async Task<(TestDbContext Context, Microsoft.Data.Sqlite.SqliteConnection Connection, User User)> SeedVerifiedUserAsync(
        string password = "StrongPass123")
    {
        var (connection, context) = TestDbContextFactory.Create();
        var passwordHasher = new PasswordHasher();

        context.Roles.Add(new Role(WellKnownRoles.ClientId, WellKnownRoles.Client));
        context.Permissions.Add(new Permission(Guid.NewGuid(), WellKnownPermissions.ContentView, "View content"));
        var permission = context.Permissions.Local.Single();
        context.RolePermissions.Add(new RolePermission(WellKnownRoles.ClientId, permission.Id));

        var user = User.Register("ada@example.com", passwordHasher.Hash(password));
        user.MarkEmailVerified(DateTime.UtcNow);
        user.AssignRole(WellKnownRoles.ClientId);
        context.Users.Add(user);

        await context.SaveChangesAsync();

        return (context, connection, user);
    }

    private static LoginHandler CreateHandler(TestDbContext context, TimeProvider timeProvider, RecordingAuditLogger? auditLogger = null) => new(
        context,
        new PasswordHasher(),
        new JwtTokenGenerator(JwtOptions),
        new SecureTokenGenerator(),
        JwtOptions,
        LockoutOptions,
        timeProvider,
        auditLogger ?? new RecordingAuditLogger(),
        NullLogger<LoginHandler>.Instance);

    [Fact]
    public async Task Valid_credentials_return_an_access_and_refresh_token_with_the_users_permissions()
    {
        var (context, connection, user) = await SeedVerifiedUserAsync();
        using var __ = connection;
        using var ___ = context;

        var auditLogger = new RecordingAuditLogger();
        var handler = CreateHandler(context, TimeProvider.System, auditLogger);

        var result = await handler.HandleAsync(new LoginCommand("ada@example.com", "StrongPass123"), CancellationToken.None);

        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.True(result.AccessTokenExpiresAtUtc > DateTime.UtcNow);

        var auditEntry = Assert.Single(auditLogger.Entries);
        Assert.Equal(AuditActions.UserLogin, auditEntry.Action);
        Assert.Equal(user.Id, auditEntry.ActorUserId);
    }

    [Fact]
    public async Task Wrong_password_throws_invalid_credentials_without_revealing_which_part_is_wrong()
    {
        var (context, connection, user) = await SeedVerifiedUserAsync();
        using var __ = connection;
        using var ___ = context;

        var auditLogger = new RecordingAuditLogger();
        var handler = CreateHandler(context, TimeProvider.System, auditLogger);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new LoginCommand("ada@example.com", "WrongPassword"), CancellationToken.None));

        Assert.Equal("INVALID_CREDENTIALS", exception.Code);

        var auditEntry = Assert.Single(auditLogger.Entries);
        Assert.Equal(AuditActions.UserFailedLogin, auditEntry.Action);
        Assert.Equal(user.Id, auditEntry.ActorUserId);
    }

    [Fact]
    public async Task Unknown_email_throws_the_same_invalid_credentials_error()
    {
        var (context, connection, _) = await SeedVerifiedUserAsync();
        using var __ = connection;
        using var ___ = context;

        var auditLogger = new RecordingAuditLogger();
        var handler = CreateHandler(context, TimeProvider.System, auditLogger);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new LoginCommand("nobody@example.com", "StrongPass123"), CancellationToken.None));

        Assert.Equal("INVALID_CREDENTIALS", exception.Code);

        var auditEntry = Assert.Single(auditLogger.Entries);
        Assert.Equal(AuditActions.UserFailedLogin, auditEntry.Action);
        Assert.Null(auditEntry.ActorUserId);
    }

    [Fact]
    public async Task Unverified_email_is_rejected()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var __ = connection;
        using var ___ = context;

        var passwordHasher = new PasswordHasher();
        var user = User.Register("ada@example.com", passwordHasher.Hash("StrongPass123"));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context, TimeProvider.System);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new LoginCommand("ada@example.com", "StrongPass123"), CancellationToken.None));

        Assert.Equal("EMAIL_NOT_VERIFIED", exception.Code);
    }

    [Fact]
    public async Task Account_locks_after_the_configured_number_of_failed_attempts_and_clears_after_cooldown()
    {
        var (context, connection, _) = await SeedVerifiedUserAsync();
        using var __ = connection;
        using var ___ = context;

        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var handler = CreateHandler(context, clock);

        for (var i = 0; i < LockoutOptions.Value.MaxFailedAttempts; i++)
        {
            await Assert.ThrowsAsync<BusinessRuleAppException>(
                () => handler.HandleAsync(new LoginCommand("ada@example.com", "WrongPassword"), CancellationToken.None));
        }

        // Locked out now: even the CORRECT password is rejected while locked.
        var lockedException = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new LoginCommand("ada@example.com", "StrongPass123"), CancellationToken.None));
        Assert.Equal("ACCOUNT_LOCKED", lockedException.Code);

        // After the cooldown, login succeeds again.
        clock.Advance(TimeSpan.FromMinutes(LockoutOptions.Value.LockoutDurationMinutes).Add(TimeSpan.FromSeconds(1)));

        var result = await handler.HandleAsync(new LoginCommand("ada@example.com", "StrongPass123"), CancellationToken.None);
        Assert.NotEmpty(result.AccessToken);
    }
}

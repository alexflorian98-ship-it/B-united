using BUnited.Modules.Identity.Domain.Entities;

namespace BUnited.Modules.Identity.Tests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Register_normalizes_email_for_uniqueness_lookups()
    {
        var user = User.Register("  Ada@Example.com  ", "hash");

        Assert.Equal("Ada@Example.com", user.Email);
        Assert.Equal("ADA@EXAMPLE.COM", user.NormalizedEmail);
    }

    [Fact]
    public void New_user_is_not_email_verified_and_not_locked_out()
    {
        var user = User.Register("ada@example.com", "hash");

        Assert.False(user.IsEmailVerified);
        Assert.False(user.IsLockedOut(DateTime.UtcNow));
    }

    [Fact]
    public void MarkEmailVerified_sets_verification_timestamp()
    {
        var user = User.Register("ada@example.com", "hash");
        var now = DateTime.UtcNow;

        user.MarkEmailVerified(now);

        Assert.True(user.IsEmailVerified);
        Assert.Equal(now, user.EmailVerifiedAtUtc);
    }

    [Fact]
    public void RegisterFailedLoginAttempt_locks_account_after_max_attempts()
    {
        var user = User.Register("ada@example.com", "hash");
        var now = DateTime.UtcNow;

        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLoginAttempt(now, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        }

        Assert.False(user.IsLockedOut(now));

        user.RegisterFailedLoginAttempt(now, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        Assert.True(user.IsLockedOut(now));
        Assert.True(user.IsLockedOut(now.AddMinutes(14)));
        Assert.False(user.IsLockedOut(now.AddMinutes(15).AddSeconds(1)));
    }

    [Fact]
    public void ResetFailedLoginAttempts_clears_lockout()
    {
        var user = User.Register("ada@example.com", "hash");
        var now = DateTime.UtcNow;

        for (var i = 0; i < 5; i++)
        {
            user.RegisterFailedLoginAttempt(now, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        }

        Assert.True(user.IsLockedOut(now));

        user.ResetFailedLoginAttempts();

        Assert.False(user.IsLockedOut(now));
        Assert.Equal(0, user.FailedLoginAttemptCount);
    }

    [Fact]
    public void AssignRole_is_idempotent()
    {
        var user = User.Register("ada@example.com", "hash");
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId);
        user.AssignRole(roleId);

        Assert.Single(user.UserRoles);
    }
}

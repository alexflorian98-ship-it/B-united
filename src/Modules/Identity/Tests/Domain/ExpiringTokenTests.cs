using BUnited.Modules.Identity.Domain.Entities;

namespace BUnited.Modules.Identity.Tests.Domain;

public sealed class ExpiringTokenTests
{
    [Fact]
    public void EmailVerificationToken_is_valid_until_expiry()
    {
        var now = DateTime.UtcNow;
        var token = EmailVerificationToken.Issue(Guid.NewGuid(), "hash", now, now.AddHours(24));

        Assert.True(token.IsValid(now));
        Assert.False(token.IsValid(now.AddHours(25)));
    }

    [Fact]
    public void EmailVerificationToken_is_invalid_once_used()
    {
        var now = DateTime.UtcNow;
        var token = EmailVerificationToken.Issue(Guid.NewGuid(), "hash", now, now.AddHours(24));

        token.MarkUsed(now);

        Assert.False(token.IsValid(now));
    }

    [Fact]
    public void PasswordResetToken_is_valid_until_expiry()
    {
        var now = DateTime.UtcNow;
        var token = PasswordResetToken.Issue(Guid.NewGuid(), "hash", now, now.AddHours(1));

        Assert.True(token.IsValid(now));
        Assert.False(token.IsValid(now.AddHours(2)));
    }

    [Fact]
    public void PasswordResetToken_is_invalid_once_used()
    {
        var now = DateTime.UtcNow;
        var token = PasswordResetToken.Issue(Guid.NewGuid(), "hash", now, now.AddHours(1));

        token.MarkUsed(now);

        Assert.False(token.IsValid(now));
    }
}

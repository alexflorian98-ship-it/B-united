using BUnited.Modules.Identity.Domain.Entities;

namespace BUnited.Modules.Identity.Tests.Domain;

public sealed class RefreshTokenTests
{
    [Fact]
    public void IssueNew_is_active_until_expiry_and_gets_a_fresh_family()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.IssueNew(Guid.NewGuid(), "hash", now, now.AddDays(30));

        Assert.True(token.IsActive(now));
        Assert.True(token.IsActive(now.AddDays(29)));
        Assert.False(token.IsActive(now.AddDays(31)));
        Assert.NotEqual(Guid.Empty, token.FamilyId);
    }

    [Fact]
    public void Revoke_deactivates_the_token()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.IssueNew(Guid.NewGuid(), "hash", now, now.AddDays(30));

        token.Revoke(now);

        Assert.False(token.IsActive(now));
        Assert.NotNull(token.RevokedAtUtc);
    }

    [Fact]
    public void IssueRotated_preserves_the_family_id()
    {
        var now = DateTime.UtcNow;
        var original = RefreshToken.IssueNew(Guid.NewGuid(), "hash-1", now, now.AddDays(30));

        var rotated = original.IssueRotated("hash-2", now, now.AddDays(30));

        Assert.Equal(original.FamilyId, rotated.FamilyId);
        Assert.NotEqual(original.Id, rotated.Id);
        Assert.Equal(original.UserId, rotated.UserId);
    }

    [Fact]
    public void Revoke_is_idempotent_and_keeps_the_first_reason()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.IssueNew(Guid.NewGuid(), "hash", now, now.AddDays(30));
        var replacementId = Guid.NewGuid();

        token.Revoke(now, replacementId);
        token.Revoke(now.AddMinutes(5), Guid.NewGuid());

        Assert.Equal(now, token.RevokedAtUtc);
        Assert.Equal(replacementId, token.ReplacedByTokenId);
    }
}

namespace BUnited.Modules.Identity.Domain.Entities;

/// <summary>
/// A rotating, revocable refresh token. Only the SHA-256 hash of the token value is ever
/// persisted (docs/DEVELOPMENT_INSTRUCTIONS.md §6) — the raw value is returned to the client
/// once, at issuance. <see cref="FamilyId"/> links every token descended from the same original
/// issuance (via rotation), so reuse of an already-rotated/revoked token can revoke the entire
/// family — see <c>RefreshTokenService</c>.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public static RefreshToken IssueNew(Guid userId, string tokenHash, DateTime issuedAtUtc, DateTime expiresAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = Guid.NewGuid(),
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };

    public RefreshToken IssueRotated(string tokenHash, DateTime issuedAtUtc, DateTime expiresAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            TokenHash = tokenHash,
            FamilyId = FamilyId,
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public Guid FamilyId { get; private set; }

    public DateTime IssuedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public void Revoke(DateTime utcNow, Guid? replacedByTokenId = null)
    {
        RevokedAtUtc ??= utcNow;
        ReplacedByTokenId ??= replacedByTokenId;
    }
}

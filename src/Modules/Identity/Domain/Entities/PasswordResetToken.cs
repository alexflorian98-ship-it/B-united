namespace BUnited.Modules.Identity.Domain.Entities;

/// <summary>
/// Single-use, expiring token consumed by the password-reset flow. Only the SHA-256 hash is
/// persisted; the raw value is emailed to the user once at issuance.
/// </summary>
public sealed class PasswordResetToken
{
    private PasswordResetToken()
    {
    }

    public static PasswordResetToken Issue(Guid userId, string tokenHash, DateTime issuedAtUtc, DateTime expiresAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime IssuedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? UsedAtUtc { get; private set; }

    public bool IsValid(DateTime utcNow) => UsedAtUtc is null && ExpiresAtUtc > utcNow;

    public void MarkUsed(DateTime utcNow) => UsedAtUtc ??= utcNow;
}

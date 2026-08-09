using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Identity.Domain.Entities;

/// <summary>
/// A platform account. Email uniqueness is enforced on <see cref="NormalizedEmail"/> (a
/// database unique index, see <c>UserConfiguration</c>) so casing differences never create
/// duplicate accounts.
/// </summary>
public sealed class User : IAuditableEntity
{
    private readonly List<UserRole> _userRoles = [];

    private User()
    {
    }

    public static User Register(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        var trimmedEmail = email.Trim();

        return new User
        {
            Id = Guid.NewGuid(),
            Email = trimmedEmail,
            NormalizedEmail = Normalize(trimmedEmail),
            PasswordHash = passwordHash,
            IsActive = true,
        };
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public DateTime? EmailVerifiedAtUtc { get; private set; }

    public DateTime? LockoutEndUtc { get; private set; }

    public int FailedLoginAttemptCount { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static string Normalize(string email) => email.Trim().ToUpperInvariant();

    public bool IsEmailVerified => EmailVerifiedAtUtc is not null;

    public void MarkEmailVerified(DateTime utcNow) => EmailVerifiedAtUtc = utcNow;

    public void ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(newPasswordHash));
        }

        PasswordHash = newPasswordHash;
    }

    public void AssignRole(Guid roleId)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId))
        {
            return;
        }

        _userRoles.Add(new UserRole(Id, roleId));
    }

    /// <summary>Idempotent-safe: removing a role the user doesn't hold is a no-op, not an error
    /// — mirrors <see cref="AssignRole"/>'s idempotency. Callers enforcing business rules (e.g.
    /// "never remove the last Administrator") must check that before calling this.</summary>
    public void RemoveRole(Guid roleId)
    {
        _userRoles.RemoveAll(ur => ur.RoleId == roleId);
    }

    public bool IsLockedOut(DateTime utcNow) => LockoutEndUtc is not null && LockoutEndUtc > utcNow;

    /// <summary>
    /// Records a failed login attempt. Once <paramref name="maxAttempts"/> is reached, the
    /// account is locked until <paramref name="utcNow"/> + <paramref name="lockoutDuration"/>.
    /// </summary>
    public void RegisterFailedLoginAttempt(DateTime utcNow, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttemptCount++;

        if (FailedLoginAttemptCount >= maxAttempts)
        {
            LockoutEndUtc = utcNow.Add(lockoutDuration);
        }
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttemptCount = 0;
        LockoutEndUtc = null;
    }

    /// <summary>Account-deletion anonymization (docs/DATA_RETENTION_POLICY.md — "Why the User
    /// row is anonymized, not deleted"): the row cannot be hard-deleted because
    /// <c>UserConsent</c> holds a <c>DeleteBehavior.Restrict</c> foreign key to it (a compliance
    /// record that must survive account deletion). <paramref name="anonymizedPasswordHash"/> MUST
    /// already be a hash of a random, discarded value the caller generated — this method does not
    /// hash anything itself. Idempotent-safe to call twice (e.g. a retried request after a
    /// transient failure), though the caller should not normally get the chance to.</summary>
    public void AnonymizeForDeletion(DateTime utcNow, string anonymizedPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(anonymizedPasswordHash))
        {
            throw new ArgumentException("An anonymized password hash is required.", nameof(anonymizedPasswordHash));
        }

        var anonymizedEmail = $"deleted-{Id:N}@deleted.bunited.local";
        Email = anonymizedEmail;
        NormalizedEmail = Normalize(anonymizedEmail);
        PasswordHash = anonymizedPasswordHash;
        IsActive = false;
        LockoutEndUtc = utcNow.AddYears(100);
    }
}

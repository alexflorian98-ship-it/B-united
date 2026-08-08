namespace BUnited.Modules.Identity.Domain.Entities;

/// <summary>
/// A versioned record of a user's consent to a specific policy (e.g. questionnaire data
/// processing, §35). Consent is append-only — a version bump requires a new row, never an
/// update to an existing one, so the full consent history is always reconstructable.
/// </summary>
public sealed class UserConsent
{
    private UserConsent()
    {
    }

    public static UserConsent Record(Guid userId, string consentType, int version, DateTime consentedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(consentType))
        {
            throw new ArgumentException("Consent type is required.", nameof(consentType));
        }

        return new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConsentType = consentType,
            Version = version,
            ConsentedAtUtc = consentedAtUtc,
        };
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string ConsentType { get; private set; } = string.Empty;

    public int Version { get; private set; }

    public DateTime ConsentedAtUtc { get; private set; }
}

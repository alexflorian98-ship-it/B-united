namespace BUnited.Modules.Audit.Contracts;

/// <summary>
/// The only way other modules hand data to <see cref="IAuditLogger"/>. Construction is the
/// API boundary (CLAUDE.md, "Cross-module dependencies go through Contracts") where metadata
/// keys are guarded against carrying secrets, tokens, or questionnaire/guidance text into a
/// persisted audit row (docs/PROMPT.md §37: "Never record secret tokens or questionnaire
/// text."; docs/DEVELOPMENT_INSTRUCTIONS.md §6). This is a defense-in-depth guard on key
/// names, not a substitute for callers choosing safe metadata in the first place.
/// </summary>
public sealed class AuditEntry
{
    private static readonly string[] ForbiddenMetadataKeyFragments =
    [
        "password",
        "token",
        "secret",
        "answer",
        "guidance",
        "questionnaire",
        "card",
        "cvv",
        "cvc",
        "ssn",
        "apikey",
        "api_key",
        "authorization",
        "credential",
    ];

    private AuditEntry(
        string action,
        Guid? actorUserId,
        string? entityType,
        string? entityId,
        string? ipAddress,
        IReadOnlyDictionary<string, string>? metadata)
    {
        Action = action;
        ActorUserId = actorUserId;
        EntityType = entityType;
        EntityId = entityId;
        IpAddress = ipAddress;
        Metadata = metadata;
    }

    public static AuditEntry Create(
        string action,
        Guid? actorUserId = null,
        string? entityType = null,
        string? entityId = null,
        string? ipAddress = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Audit action is required.", nameof(action));
        }

        if (metadata is not null)
        {
            foreach (var key in metadata.Keys)
            {
                foreach (var fragment in ForbiddenMetadataKeyFragments)
                {
                    if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException(
                            $"Audit metadata key '{key}' is not allowed: it may carry a secret, " +
                            "token, or questionnaire/guidance payload (docs/PROMPT.md §37).",
                            nameof(metadata));
                    }
                }
            }
        }

        return new AuditEntry(action, actorUserId, entityType, entityId, ipAddress, metadata);
    }

    public string Action { get; }

    public Guid? ActorUserId { get; }

    public string? EntityType { get; }

    public string? EntityId { get; }

    public string? IpAddress { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }
}

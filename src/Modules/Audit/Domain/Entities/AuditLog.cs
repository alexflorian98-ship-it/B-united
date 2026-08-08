namespace BUnited.Modules.Audit.Domain.Entities;

/// <summary>
/// An immutable, append-only record of a business-critical or security-relevant action
/// (docs/PROMPT.md §37). Deliberately has no reference to <c>User</c> or any other module's
/// Domain entity — <see cref="ActorUserId"/> is an opaque identifier only, per the modular
/// monolith's cross-module boundary rules (CLAUDE.md "Cross-module dependencies go through
/// Contracts"). Never construct this with secret tokens, questionnaire answers, or guidance
/// text in <see cref="MetadataJson"/> — see <c>BUnited.Modules.Audit.Contracts.AuditEntry</c>,
/// which guards the key names allowed into metadata before it ever reaches this entity.
/// </summary>
public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public static AuditLog Create(
        string action,
        DateTime timestampUtc,
        Guid? actorUserId,
        string? entityType,
        string? entityId,
        string? correlationId,
        string? ipAddress,
        string? metadataJson) =>
        new()
        {
            Id = Guid.NewGuid(),
            Action = action,
            TimestampUtc = timestampUtc,
            ActorUserId = actorUserId,
            EntityType = entityType,
            EntityId = entityId,
            CorrelationId = correlationId,
            IpAddress = ipAddress,
            MetadataJson = metadataJson,
        };

    public Guid Id { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid? ActorUserId { get; private set; }

    public string? EntityType { get; private set; }

    public string? EntityId { get; private set; }

    public DateTime TimestampUtc { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? IpAddress { get; private set; }

    public string? MetadataJson { get; private set; }
}

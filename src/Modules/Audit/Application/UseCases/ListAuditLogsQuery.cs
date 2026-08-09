namespace BUnited.Modules.Audit.Application.UseCases;

/// <summary>All filters are optional and combine with AND. <paramref name="FromUtc"/>/
/// <paramref name="ToUtc"/> bound <see cref="Domain.Entities.AuditLog.TimestampUtc"/>
/// inclusively.</summary>
public sealed record ListAuditLogsQuery(
    string? Action,
    Guid? ActorUserId,
    string? EntityType,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize);

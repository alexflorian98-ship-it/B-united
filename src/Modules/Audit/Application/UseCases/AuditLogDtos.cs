namespace BUnited.Modules.Audit.Application.UseCases;

public sealed record AuditLogEntryDto(
    Guid Id,
    string Action,
    DateTime TimestampUtc,
    Guid? ActorUserId,
    string? ActorEmail,
    string? EntityType,
    string? EntityId,
    string? CorrelationId,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record AuditLogListResult(IReadOnlyList<AuditLogEntryDto> Items, int TotalCount, int Page, int PageSize);

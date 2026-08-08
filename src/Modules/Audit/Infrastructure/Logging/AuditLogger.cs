using System.Text.Json;
using BUnited.BuildingBlocks.Observability.CorrelationId;
using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Audit.Infrastructure.Logging;

/// <summary>
/// Writes and persists an <see cref="AuditLog"/> row on every call, independently of whatever
/// unit of work the caller is in the middle of — an audit write must not silently disappear
/// because a caller forgot to call <c>SaveChangesAsync</c> afterwards.
/// </summary>
public sealed class AuditLogger(
    DbContext dbContext,
    ICorrelationIdAccessor correlationIdAccessor,
    TimeProvider timeProvider) : IAuditLogger
{
    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        var metadataJson = entry.Metadata is { Count: > 0 }
            ? JsonSerializer.Serialize(entry.Metadata)
            : null;

        var auditLog = AuditLog.Create(
            entry.Action,
            timeProvider.GetUtcNow().UtcDateTime,
            entry.ActorUserId,
            entry.EntityType,
            entry.EntityId,
            correlationIdAccessor.CorrelationId,
            entry.IpAddress,
            metadataJson);

        dbContext.Set<AuditLog>().Add(auditLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

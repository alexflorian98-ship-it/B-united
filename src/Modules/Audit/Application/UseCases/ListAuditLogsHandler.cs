using System.Text.Json;
using BUnited.Modules.Audit.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Audit.Application.UseCases;

/// <summary>docs/IMPLEMENTATION_PLAN.md Slice A4 — the audit trail read side (docs/PROMPT.md
/// §37). Reads only its own module's <see cref="AuditLog"/> table (no cross-module boundary
/// crossed for the log rows themselves); resolves actor emails through
/// <see cref="IUserLookup"/> for display, the same read-only cross-module pattern the Admin
/// dashboard uses. Every metadata key was already guarded against secrets/tokens/questionnaire
/// content at write time (<c>AuditEntry.Create</c>) — nothing further to filter here.</summary>
public sealed class ListAuditLogsHandler(DbContext dbContext, IUserLookup userLookup)
{
    public async Task<AuditLogListResult> HandleAsync(ListAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.Set<AuditLog>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            baseQuery = baseQuery.Where(a => a.Action == query.Action);
        }

        if (query.ActorUserId is { } actorUserId)
        {
            baseQuery = baseQuery.Where(a => a.ActorUserId == actorUserId);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            baseQuery = baseQuery.Where(a => a.EntityType == query.EntityType);
        }

        if (query.FromUtc is { } fromUtc)
        {
            baseQuery = baseQuery.Where(a => a.TimestampUtc >= fromUtc);
        }

        if (query.ToUtc is { } toUtc)
        {
            baseQuery = baseQuery.Where(a => a.TimestampUtc <= toUtc);
        }

        baseQuery = baseQuery.OrderByDescending(a => a.TimestampUtc);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var page = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var actorIds = page.Where(a => a.ActorUserId is not null).Select(a => a.ActorUserId!.Value).Distinct().ToList();
        var actors = await userLookup.GetByIdsAsync(actorIds, cancellationToken);

        var items = page.Select(a => new AuditLogEntryDto(
            a.Id,
            a.Action,
            a.TimestampUtc,
            a.ActorUserId,
            a.ActorUserId is { } id && actors.TryGetValue(id, out var actor) ? actor.Email : null,
            a.EntityType,
            a.EntityId,
            a.CorrelationId,
            DeserializeMetadata(a.MetadataJson))).ToList();

        return new AuditLogListResult(items, totalCount, query.Page, query.PageSize);
    }

    private static IReadOnlyDictionary<string, string>? DeserializeMetadata(string? metadataJson) =>
        metadataJson is null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
}

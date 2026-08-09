using BUnited.Modules.Audit.Application.UseCases;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Audit.Api.Controllers;

/// <summary>docs/IMPLEMENTATION_PLAN.md Slice A4 — closes the "Audit" admin placeholder with a
/// real, permission-protected, paginated audit trail read (docs/PROMPT.md §37). Filterable by
/// action, actor, entity type, and a UTC date range.</summary>
[ApiController]
[Route("api/v1/admin/audit")]
[Authorize(Policy = WellKnownPermissionKeys.AuditView)]
public sealed class AdminAuditController(ListAuditLogsHandler listAuditLogsHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuditLogListResult>> List(
        [FromQuery] string? action,
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? entityType,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > 100 ? 25 : pageSize;

        var result = await listAuditLogsHandler.HandleAsync(
            new ListAuditLogsQuery(action, actorUserId, entityType, fromUtc, toUtc, normalizedPage, normalizedPageSize),
            cancellationToken);
        return Ok(result);
    }
}

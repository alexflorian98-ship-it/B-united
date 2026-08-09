using BUnited.Modules.Admin.Application.Dtos;
using BUnited.Modules.Admin.Application.UseCases;
using BUnited.Modules.Admin.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Admin.Api.Controllers;

/// <summary>docs/PROMPT.md §442 — the expert/admin dashboard: KPI cards plus the "requires
/// attention" widgets (pending questionnaires, upcoming events, recent purchases/refunds,
/// reported chat messages, recently published content). See ADR-007 and
/// <see cref="GetDashboardHandler"/> for why this is allowed to query multiple modules'
/// tables directly while staying strictly read-only.</summary>
[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Policy = AdminPermissionPolicies.DashboardView)]
public sealed class AdminDashboardController(GetDashboardHandler getDashboardHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminDashboardDto>> Get(CancellationToken cancellationToken)
    {
        var result = await getDashboardHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}

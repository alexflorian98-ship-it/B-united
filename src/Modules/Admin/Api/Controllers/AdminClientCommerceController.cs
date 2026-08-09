using BUnited.Modules.Admin.Application.Dtos;
using BUnited.Modules.Admin.Application.UseCases;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Admin.Api.Controllers;

/// <summary>The purchases/entitlements section of the client detail screen
/// (docs/IMPLEMENTATION_PLAN.md Slice A3) — separate from
/// <c>BUnited.Modules.Identity.Api.Controllers.AdminUsersController</c> because it crosses into
/// Billing's tables and only the Admin module is allowed to do that read-only cross-module join
/// (ADR-007).</summary>
[ApiController]
[Route("api/v1/admin/clients/{userId:guid}/commerce-summary")]
[Authorize(Policy = WellKnownPermissionKeys.UsersManage)]
public sealed class AdminClientCommerceController(GetClientCommerceSummaryHandler getClientCommerceSummaryHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ClientCommerceSummaryDto>> Get(Guid userId, CancellationToken cancellationToken)
    {
        var result = await getClientCommerceSummaryHandler.HandleAsync(userId, cancellationToken);
        return Ok(result);
    }
}

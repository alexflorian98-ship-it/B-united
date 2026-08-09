using BUnited.Modules.Identity.Application.UseCases.Admin.Users;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Identity.Api.Controllers;

/// <summary>docs/IMPLEMENTATION_PLAN.md Slice A3 — real client administration (list, detail,
/// role assignment) replacing the "Subscribers" placeholder. Every action requires
/// <see cref="WellKnownPermissionKeys.UsersManage"/>; role changes are metadata-only audited
/// (<c>user.role_changed</c>) and protect the last remaining Administrator from being
/// de-roled.</summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = WellKnownPermissionKeys.UsersManage)]
public sealed class AdminUsersController(
    ListClientsHandler listClientsHandler,
    GetClientDetailHandler getClientDetailHandler,
    AssignClientRoleHandler assignClientRoleHandler,
    RemoveClientRoleHandler removeClientRoleHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ClientListResult>> List(
        [FromQuery] string? search,
        [FromQuery] Guid? roleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var result = await listClientsHandler.HandleAsync(
            new ListClientsQuery(search, roleId, normalizedPage, normalizedPageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<ClientDetailDto>> Get(Guid userId, CancellationToken cancellationToken)
    {
        var result = await getClientDetailHandler.HandleAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{userId:guid}/roles")]
    public async Task<IActionResult> AssignRole(Guid userId, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        await assignClientRoleHandler.HandleAsync(
            new AssignClientRoleCommand(User.GetUserId(), userId, request.RoleId),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        await removeClientRoleHandler.HandleAsync(
            new RemoveClientRoleCommand(User.GetUserId(), userId, roleId),
            cancellationToken);
        return NoContent();
    }
}

/// <summary>Shares the route prefix with <see cref="AdminUsersController"/> conceptually but is
/// registered separately since <c>/api/v1/admin/roles</c> is not nested under a user id — backs
/// the role filter dropdown and the role-assignment control on the client screens.</summary>
[ApiController]
[Route("api/v1/admin/roles")]
[Authorize(Policy = WellKnownPermissionKeys.UsersManage)]
public sealed class AdminRolesController(ListRolesHandler listRolesHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var result = await listRolesHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}

public sealed record AssignRoleRequest(Guid RoleId);

using BUnited.Modules.Identity.Application.UseCases.DataRights;
using BUnited.Modules.Identity.Application.UseCases.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public sealed class ProfileController(
    GetProfileHandler getProfileHandler,
    UpdateProfileHandler updateProfileHandler,
    ExportMyDataHandler exportMyDataHandler,
    DeleteMyAccountHandler deleteMyAccountHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileResult>> Get(CancellationToken cancellationToken)
    {
        var result = await getProfileHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResult>> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await updateProfileHandler.HandleAsync(
            new UpdateProfileCommand(User.GetUserId(), request.Timezone, request.PreferredLanguage, request.EmailNotificationsEnabled),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Self-service full-account data export (docs/PROMPT.md §66). Always scoped to the
    /// authenticated caller's own data — there is no admin-supplied user id path.</summary>
    [HttpGet("export")]
    public async Task<ActionResult<UserDataExportDto>> Export(CancellationToken cancellationToken)
    {
        var result = await exportMyDataHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Self-service, irreversible account deletion (docs/PROMPT.md §66,
    /// docs/DATA_RETENTION_POLICY.md). Requires the caller's current password as an explicit
    /// confirmation step so this can never be triggered by a stray click or CSRF alone.</summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete(DeleteMyAccountRequest request, CancellationToken cancellationToken)
    {
        await deleteMyAccountHandler.HandleAsync(new DeleteMyAccountCommand(User.GetUserId(), request.CurrentPassword), cancellationToken);
        return NoContent();
    }
}

public sealed record DeleteMyAccountRequest(string CurrentPassword);

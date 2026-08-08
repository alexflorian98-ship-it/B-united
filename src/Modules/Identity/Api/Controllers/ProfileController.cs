using BUnited.Modules.Identity.Application.UseCases.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public sealed class ProfileController(GetProfileHandler getProfileHandler, UpdateProfileHandler updateProfileHandler) : ControllerBase
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
}

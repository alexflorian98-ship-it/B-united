using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Application.UseCases.Admin;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Billing.Api.Controllers;

/// <summary>§54 admin billing UI. Raw webhook payload visibility (P3.22) is restricted to
/// <see cref="WellKnownPermissionKeys.BillingViewRawWebhookPayloads"/> holders — checked here,
/// not left to the frontend to hide.</summary>
[ApiController]
[Route("api/v1/admin/billing")]
[Authorize(Policy = WellKnownPermissionKeys.BillingManage)]
public sealed class AdminBillingController(ListSubscribersHandler listSubscribersHandler, GetSubscriptionDetailHandler getSubscriptionDetailHandler) : ControllerBase
{
    [HttpGet("subscribers")]
    public async Task<ActionResult<SubscriberListResult>> ListSubscribers([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var result = await listSubscribersHandler.HandleAsync(new ListSubscribersQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("subscribers/{subscriptionId:guid}")]
    public async Task<ActionResult<SubscriptionDetailDto>> GetSubscription(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var includeRawPayload = User.HasClaim("permission", WellKnownPermissionKeys.BillingViewRawWebhookPayloads);
        var result = await getSubscriptionDetailHandler.HandleAsync(subscriptionId, includeRawPayload, cancellationToken);
        return Ok(result);
    }
}

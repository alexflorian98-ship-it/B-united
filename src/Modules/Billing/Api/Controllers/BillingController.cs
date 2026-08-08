using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Application.UseCases;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Billing.Api.Controllers;

/// <summary>Client-facing billing (§17, P3.06/P3.14/P3.17–19). Demo-only controls (P3.18) are
/// clearly separated into their own action so the "simulated payment" nature is unambiguous —
/// see ADR-010.</summary>
[ApiController]
[Route("api/v1/billing")]
[Authorize(Policy = WellKnownPermissionKeys.BillingView)]
public sealed class BillingController(
    ListPlansHandler listPlansHandler,
    GetMyBillingStatusHandler getMyBillingStatusHandler,
    CreateCheckoutHandler createCheckoutHandler,
    TriggerDemoEventHandler triggerDemoEventHandler) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> ListPlans(CancellationToken cancellationToken)
    {
        var result = await listPlansHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<ActionResult<MyBillingStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await getMyBillingStatusHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResultDto>> Checkout(CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        var result = await createCheckoutHandler.HandleAsync(new CreateCheckoutCommand(User.GetUserId(), request.PlanPriceId, request.Outcome), cancellationToken);
        return Ok(result);
    }

    /// <summary>P3.18: demo-only subscription controls. Never available in `Production` — see
    /// `AddBillingModule`'s `FakePaymentProvider` registration and the P3.32 startup gate.</summary>
    [HttpPost("demo/{demoAction}")]
    public async Task<IActionResult> TriggerDemoEvent(DemoAction demoAction, CancellationToken cancellationToken)
    {
        await triggerDemoEventHandler.HandleAsync(new TriggerDemoEventCommand(User.GetUserId(), demoAction), cancellationToken);
        return NoContent();
    }
}

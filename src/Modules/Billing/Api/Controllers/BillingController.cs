using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Application.UseCases;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Billing.Api.Controllers;

/// <summary>Client-facing billing (§17, P3.37–39): per-program one-time purchases, not a global
/// subscription. Demo-only controls (P3.18) are clearly separated into their own action so the
/// "simulated payment" nature is unambiguous — see ADR-010.</summary>
[ApiController]
[Route("api/v1/billing")]
[Authorize(Policy = WellKnownPermissionKeys.BillingView)]
public sealed class BillingController(
    ListMyPurchasesHandler listMyPurchasesHandler,
    ListMyInvoicesHandler listMyInvoicesHandler,
    GetMyInvoiceHandler getMyInvoiceHandler,
    ListMyEntitlementsHandler listMyEntitlementsHandler,
    CreateProgramPurchaseHandler createProgramPurchaseHandler,
    TriggerDemoEventHandler triggerDemoEventHandler) : ControllerBase
{
    [HttpGet("my-purchases")]
    public async Task<ActionResult<IReadOnlyList<PurchaseDto>>> ListMyPurchases(CancellationToken cancellationToken)
    {
        var result = await listMyPurchasesHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-invoices")]
    public async Task<ActionResult<IReadOnlyList<MyInvoiceDto>>> ListMyInvoices(CancellationToken cancellationToken)
    {
        var result = await listMyInvoicesHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    /// <summary>P3.19.b: single-invoice detail/receipt view, reachable from a row in the
    /// my-invoices list. Ownership-scoped in the handler — a mismatched or unknown invoice both
    /// surface as 404, never a 403 (CLAUDE.md never-confirm-existence rule).</summary>
    [HttpGet("my-invoices/{invoiceId:guid}")]
    public async Task<ActionResult<MyInvoiceDto>> GetMyInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await getMyInvoiceHandler.HandleAsync(User.GetUserId(), invoiceId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-entitlements")]
    public async Task<ActionResult<IReadOnlyList<ProgramEntitlementDto>>> ListMyEntitlements(CancellationToken cancellationToken)
    {
        var result = await listMyEntitlementsHandler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("programs/{programId:guid}/checkout")]
    public async Task<ActionResult<CheckoutResultDto>> Checkout(Guid programId, CreateProgramPurchaseRequest request, CancellationToken cancellationToken)
    {
        var result = await createProgramPurchaseHandler.HandleAsync(new CreateProgramPurchaseCommand(User.GetUserId(), programId, request.Outcome), cancellationToken);
        return Ok(result);
    }

    /// <summary>P3.18: demo-only purchase controls. Never available in `Production` — see
    /// `AddBillingModule`'s `FakePaymentProvider` registration and the P3.32 startup gate.</summary>
    [HttpPost("programs/{programId:guid}/demo/{demoAction}")]
    public async Task<IActionResult> TriggerDemoEvent(Guid programId, DemoAction demoAction, CancellationToken cancellationToken)
    {
        await triggerDemoEventHandler.HandleAsync(new TriggerDemoEventCommand(User.GetUserId(), programId, demoAction), cancellationToken);
        return NoContent();
    }
}

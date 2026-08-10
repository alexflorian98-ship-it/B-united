using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Application.UseCases.Admin;
using BUnited.Modules.Billing.Domain;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Billing.Api.Controllers;

/// <summary>§54 admin billing UI. Raw webhook payload visibility (P3.22) is restricted to
/// <see cref="WellKnownPermissionKeys.BillingViewRawWebhookPayloads"/> holders — checked here,
/// not left to the frontend to hide. Program-offer management (P3.35) shares the same
/// <see cref="WellKnownPermissionKeys.BillingManage"/> policy as the rest of this controller.</summary>
[ApiController]
[Route("api/v1/admin/billing")]
[Authorize(Policy = WellKnownPermissionKeys.BillingManage)]
public sealed class AdminBillingController(
    ListPurchasesHandler listPurchasesHandler,
    GetPurchaseDetailHandler getPurchaseDetailHandler,
    CreateProgramOfferHandler createProgramOfferHandler,
    UpdateProgramOfferPriceHandler updateProgramOfferPriceHandler,
    ProgramOfferStatusHandler programOfferStatusHandler,
    ListProgramOffersHandler listProgramOffersHandler,
    GetProgramOfferDetailHandler getProgramOfferDetailHandler) : ControllerBase
{
    [HttpGet("purchases")]
    public async Task<ActionResult<PurchaseListResult>> ListPurchases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] PurchaseStatus? status = null,
        [FromQuery] Guid? programId = null,
        [FromQuery] PurchaseSortBy sortBy = PurchaseSortBy.CreatedAt,
        [FromQuery] bool descending = true,
        CancellationToken cancellationToken = default)
    {
        var result = await listPurchasesHandler.HandleAsync(new ListPurchasesQuery(page, pageSize, status, programId, sortBy, descending), cancellationToken);
        return Ok(result);
    }

    [HttpGet("purchases/{purchaseId:guid}")]
    public async Task<ActionResult<PurchaseDetailDto>> GetPurchase(Guid purchaseId, CancellationToken cancellationToken)
    {
        var includeRawPayload = User.HasClaim("permission", WellKnownPermissionKeys.BillingViewRawWebhookPayloads);
        var result = await getPurchaseDetailHandler.HandleAsync(purchaseId, includeRawPayload, cancellationToken);
        return Ok(result);
    }

    [HttpGet("offers")]
    public async Task<ActionResult<ProgramOfferListResult>> ListOffers(
        [FromQuery] ProgramOfferStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await listProgramOffersHandler.HandleAsync(new ListProgramOffersQuery(status, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("offers/{offerId:guid}")]
    public async Task<ActionResult<ProgramOfferDetailDto>> GetOffer(Guid offerId, CancellationToken cancellationToken)
    {
        var result = await getProgramOfferDetailHandler.HandleAsync(offerId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("offers")]
    public async Task<ActionResult<CreateProgramOfferResultDto>> CreateOffer(CreateProgramOfferRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProgramOfferCommand(request.ProgramId, request.Amount, request.Currency, request.ActivateImmediately, User.GetUserId());
        var result = await createProgramOfferHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetOffer), new { offerId = result.ProgramOfferId }, result);
    }

    [HttpPut("offers/{offerId:guid}/price")]
    public async Task<ActionResult<Guid>> UpdateOfferPrice(Guid offerId, UpdateProgramOfferPriceRequest request, CancellationToken cancellationToken)
    {
        var priceId = await updateProgramOfferPriceHandler.HandleAsync(
            new UpdateProgramOfferPriceCommand(offerId, request.Amount, request.Currency, User.GetUserId()),
            cancellationToken);
        return Ok(priceId);
    }

    [HttpPost("offers/{offerId:guid}/activate")]
    public async Task<IActionResult> ActivateOffer(Guid offerId, CancellationToken cancellationToken)
    {
        await programOfferStatusHandler.ActivateAsync(offerId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("offers/{offerId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateOffer(Guid offerId, CancellationToken cancellationToken)
    {
        await programOfferStatusHandler.DeactivateAsync(offerId, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}

using System.Text;
using System.Text.Json;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Application.Configuration;
using BUnited.Modules.Billing.Application.UseCases;
using BUnited.Modules.Billing.Infrastructure.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Billing.Api.Controllers;

public sealed record FakeWebhookEventRequest(string ProviderEventId, string Type, Guid PurchaseId, decimal? Amount, string? Currency, DateTime ProviderTimestampUtc);

/// <summary>P3.07: the demo fake-provider event endpoint. Deliberately unauthenticated (no
/// `[Authorize]`) — a real webhook endpoint never carries a user's bearer token either; trust
/// here comes entirely from the demo HMAC signature (P3.07.a/b), and the whole endpoint is
/// hard-disabled outside `Development`/`Demo` (P3.07.c) regardless of signature validity.
/// ADR-010: this exercises the exact idempotency/out-of-order/state-machine risk area §68 calls
/// out as highest-risk, just against a local simulation instead of a real provider.</summary>
[ApiController]
[Route("api/v1/billing/webhooks/fake")]
public sealed class FakeWebhookController(
    IWebHostEnvironment environment,
    IOptions<BillingOptions> billingOptions,
    ProcessProviderEventHandler processProviderEventHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && environment.EnvironmentName != "Demo")
        {
            return NotFound();
        }

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var providedSignature = Request.Headers["X-Demo-Signature"].ToString();
        if (!DemoWebhookSignature.Verify(billingOptions.Value.DemoWebhookSecret, rawBody, providedSignature))
        {
            return Unauthorized();
        }

        FakeWebhookEventRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<FakeWebhookEventRequest>(rawBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        if (payload is null || !Enum.TryParse<ProviderEventType>(payload.Type, out var eventType))
        {
            return BadRequest();
        }

        var providerEvent = new ProviderEvent(payload.ProviderEventId, eventType, payload.PurchaseId, payload.Amount, payload.Currency, payload.ProviderTimestampUtc, rawBody);
        await processProviderEventHandler.HandleAsync(providerEvent, cancellationToken);

        return NoContent();
    }
}

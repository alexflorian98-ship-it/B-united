using BUnited.Modules.Billing.Application.Abstractions;

namespace BUnited.Modules.Billing.Application.UseCases;

public sealed record CreateCheckoutRequest(Guid PlanPriceId, CheckoutOutcome Outcome = CheckoutOutcome.Success);

public sealed record CreateCheckoutCommand(Guid UserId, Guid PlanPriceId, CheckoutOutcome Outcome);

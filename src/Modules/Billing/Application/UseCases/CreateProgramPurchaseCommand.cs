using BUnited.Modules.Billing.Application.Abstractions;

namespace BUnited.Modules.Billing.Application.UseCases;

public sealed record CreateProgramPurchaseRequest(CheckoutOutcome Outcome = CheckoutOutcome.Success);

public sealed record CreateProgramPurchaseCommand(Guid UserId, Guid ProgramId, CheckoutOutcome Outcome);

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

public sealed record CreateProgramOfferRequest(Guid ProgramId, decimal Amount, string Currency, bool ActivateImmediately = false);

public sealed record CreateProgramOfferCommand(Guid ProgramId, decimal Amount, string Currency, bool ActivateImmediately, Guid ActorId);

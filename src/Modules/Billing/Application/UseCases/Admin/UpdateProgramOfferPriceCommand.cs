namespace BUnited.Modules.Billing.Application.UseCases.Admin;

public sealed record UpdateProgramOfferPriceRequest(decimal Amount, string Currency);

public sealed record UpdateProgramOfferPriceCommand(Guid ProgramOfferId, decimal Amount, string Currency, Guid ActorId);

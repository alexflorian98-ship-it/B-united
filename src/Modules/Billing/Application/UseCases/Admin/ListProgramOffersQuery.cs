using BUnited.Modules.Billing.Domain;

namespace BUnited.Modules.Billing.Application.UseCases.Admin;

public sealed record ListProgramOffersQuery(ProgramOfferStatus? Status, int Page, int PageSize);

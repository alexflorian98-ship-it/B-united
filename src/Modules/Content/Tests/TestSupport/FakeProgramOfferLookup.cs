using BUnited.Modules.Billing.Contracts;

namespace BUnited.Modules.Content.Tests.TestSupport;

/// <summary>Test double for Billing's cross-module <see cref="IProgramOfferLookup"/> — Content's
/// tests must never depend on Billing's real Domain/Infrastructure (CLAUDE.md), so the active
/// offer per program is configured directly by the test instead.</summary>
internal sealed class FakeProgramOfferLookup : IProgramOfferLookup
{
    private readonly Dictionary<Guid, ActiveOfferSummary> _offersByProgramId = [];

    public void SetActiveOffer(Guid programId, decimal amount, string currency) =>
        _offersByProgramId[programId] = new ActiveOfferSummary(Guid.NewGuid(), Guid.NewGuid(), amount, currency);

    public Task<ActiveOfferSummary?> GetActiveOfferAsync(Guid programId, CancellationToken cancellationToken) =>
        Task.FromResult(_offersByProgramId.GetValueOrDefault(programId));
}

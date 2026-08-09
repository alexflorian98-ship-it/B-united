namespace BUnited.Modules.Billing.Contracts;

/// <summary>Read-only cross-module lookup so another module (e.g. Content's catalogue, later
/// phases) can show a program's current price without referencing Billing's Domain/
/// Infrastructure layers directly (CLAUDE.md). Also used in-module by checkout so the server
/// alone decides which offer/price applies — the client only ever sends a <c>ProgramId</c>.</summary>
public interface IProgramOfferLookup
{
    Task<ActiveOfferSummary?> GetActiveOfferAsync(Guid programId, CancellationToken cancellationToken);

    /// <summary>Batch form of <see cref="GetActiveOfferAsync"/> for callers resolving offers for many
    /// programs at once (e.g. the client catalogue list) — avoids one round trip per program. The
    /// default implementation preserves correctness for any existing implementer by falling back to
    /// per-program calls; <c>ProgramOfferLookup</c> overrides it with a single batched query.</summary>
    async Task<IReadOnlyDictionary<Guid, ActiveOfferSummary>> GetActiveOffersAsync(
        IReadOnlyCollection<Guid> programIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, ActiveOfferSummary>();
        foreach (var programId in programIds)
        {
            var offer = await GetActiveOfferAsync(programId, cancellationToken);
            if (offer is not null)
            {
                result[programId] = offer;
            }
        }

        return result;
    }
}

public sealed record ActiveOfferSummary(Guid ProgramOfferId, Guid ProgramPriceId, decimal Amount, string Currency);

namespace BUnited.BuildingBlocks.Application.DataRights;

/// <summary>
/// Cross-module contract for the account-deletion workflow (docs/PROMPT.md §66,
/// docs/DATA_RETENTION_POLICY.md). Implemented by a module's Application layer for data it owns
/// that must be hard-deleted or anonymized when a user deletes their account. Resolved via DI as
/// <c>IEnumerable&lt;IUserDataEraser&gt;</c> by the orchestrating deletion handler (Identity).
///
/// Implementations MUST stage changes (add/remove/update) on the shared <c>DbContext</c> only —
/// they MUST NOT call <c>SaveChangesAsync</c> themselves. Because every module shares one
/// <c>DbContext</c>/connection in this modular monolith, the orchestrating handler commits every
/// participant's staged changes in a single transaction
/// (docs/DEVELOPMENT_INSTRUCTIONS.md §4/§2, "mutations that can leave partial business state MUST
/// use an explicit transaction").
///
/// Modules whose data must be retained rather than erased (Billing purchases/payments/invoices,
/// per docs/PROMPT.md §66 and docs/DATA_RETENTION_POLICY.md) deliberately do not implement this
/// interface. Modules that must preserve rows but scrub/decouple the identity reference (Chat
/// messages — "do not destroy conversation continuity") implement it to erase only the
/// user-linked side records, not the retained content.
/// </summary>
public interface IUserDataEraser
{
    Task EraseAsync(Guid userId, CancellationToken cancellationToken);
}

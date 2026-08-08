namespace BUnited.Modules.Audit.Contracts;

/// <summary>
/// Write-only append API for the audit trail (docs/PROMPT.md §37), usable from any module.
/// There is intentionally no read method here: audit reads are a separate, explicitly
/// authorized concern (admin/dashboard read models), not something every module should be
/// able to query.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken);
}

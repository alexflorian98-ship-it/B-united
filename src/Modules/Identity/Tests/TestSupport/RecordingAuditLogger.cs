using BUnited.Modules.Audit.Contracts;

namespace BUnited.Modules.Identity.Tests.TestSupport;

internal sealed class RecordingAuditLogger : IAuditLogger
{
    private readonly List<AuditEntry> _entries = [];

    public IReadOnlyList<AuditEntry> Entries => _entries;

    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }
}

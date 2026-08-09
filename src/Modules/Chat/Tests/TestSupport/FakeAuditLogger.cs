using BUnited.Modules.Audit.Contracts;

namespace BUnited.Modules.Chat.Tests.TestSupport;

internal sealed class FakeAuditLogger : IAuditLogger
{
    public List<AuditEntry> Entries { get; } = [];

    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

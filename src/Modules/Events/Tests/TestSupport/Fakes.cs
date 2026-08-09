using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Identity.Contracts;
using BUnited.Modules.Notifications.Contracts;

namespace BUnited.Modules.Events.Tests.TestSupport;

internal sealed record SentNotification(NotificationType Type, string RecipientEmail, IReadOnlyDictionary<string, string> TemplateData);

internal sealed class FakeNotificationSender : INotificationSender
{
    public List<SentNotification> Sent { get; } = [];

    public Task SendAsync(NotificationType type, string recipientEmail, IReadOnlyDictionary<string, string> templateData, CancellationToken cancellationToken)
    {
        Sent.Add(new SentNotification(type, recipientEmail, templateData));
        return Task.CompletedTask;
    }
}

internal sealed class FakeUserLookup : IUserLookup
{
    public Dictionary<Guid, UserSummary> Users { get; } = [];

    public Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Users.GetValueOrDefault(userId));

    public Task<IReadOnlyDictionary<Guid, UserSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, UserSummary>>(Users.Where(kv => userIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
}

internal sealed class FakeNotificationPreferenceLookup : INotificationPreferenceLookup
{
    public HashSet<Guid> OptedOutUsers { get; } = [];

    public Task<bool> AreEmailNotificationsEnabledAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(!OptedOutUsers.Contains(userId));
}

internal sealed class FakeAuditLogger : IAuditLogger
{
    public List<AuditEntry> Entries { get; } = [];

    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

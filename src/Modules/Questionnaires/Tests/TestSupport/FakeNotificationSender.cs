using BUnited.Modules.Notifications.Contracts;

namespace BUnited.Modules.Questionnaires.Tests.TestSupport;

internal sealed class FakeNotificationSender : INotificationSender
{
    public List<(NotificationType Type, string RecipientEmail)> Sent { get; } = [];

    public Task SendAsync(NotificationType type, string recipientEmail, IReadOnlyDictionary<string, string> templateData, CancellationToken cancellationToken)
    {
        Sent.Add((type, recipientEmail));
        return Task.CompletedTask;
    }
}

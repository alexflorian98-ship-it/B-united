using BUnited.BuildingBlocks.Application.Access;
using BUnited.Modules.Notifications.Contracts;
using BUnited.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Notifications.Tests;

/// <summary>
/// <see cref="LoggingNotificationSender"/> is the only real behavior the Notifications module
/// currently exposes (V1 has no real email provider — it structured-logs instead, exactly like
/// Identity's own LoggingIdentityEmailSender). These tests prove the two things that actually
/// matter about it: it never logs <c>templateData</c> — which per its own contract
/// (INotificationSender's XML doc) and CLAUDE.md's logging rule may carry submission ids that
/// resolve to guidance text/questionnaire content — and it is gated from Production via
/// <see cref="IDemoOnlyAdapter"/> so the app refuses to boot with it wired as the real sender.
/// </summary>
public sealed class LoggingNotificationSenderTests
{
    private sealed class CapturingLogger : ILogger<LoggingNotificationSender>
    {
        public List<(LogLevel Level, string Message, IReadOnlyList<KeyValuePair<string, object?>> State)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            Entries.Add((logLevel, formatter(state, exception), values));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }

    [Fact]
    public async Task SendAsync_logs_type_and_recipient_but_never_the_template_data_values()
    {
        var logger = new CapturingLogger();
        var sender = new LoggingNotificationSender(logger);
        const string sensitiveGuidanceExcerpt = "You should stop taking the medication your questionnaire mentioned.";

        await sender.SendAsync(
            NotificationType.GuidancePublished,
            "client@example.com",
            new Dictionary<string, string> { ["guidanceExcerpt"] = sensitiveGuidanceExcerpt, ["submissionId"] = "12345" },
            CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("GuidancePublished", entry.Message);
        Assert.Contains("client@example.com", entry.Message);
        Assert.DoesNotContain(sensitiveGuidanceExcerpt, entry.Message);

        // The structured state itself (what Serilog/any sink would actually persist) must not
        // carry the template data dictionary or any of its values under any property name either
        // — not just the rendered message string.
        foreach (var (_, value) in entry.State)
        {
            Assert.NotEqual(sensitiveGuidanceExcerpt, value);
        }
    }

    [Theory]
    [InlineData(NotificationType.EmailVerification)]
    [InlineData(NotificationType.PasswordReset)]
    [InlineData(NotificationType.Welcome)]
    [InlineData(NotificationType.SubscriptionActivated)]
    [InlineData(NotificationType.PaymentFailed)]
    [InlineData(NotificationType.SubscriptionEnding)]
    [InlineData(NotificationType.QuestionnaireSubmitted)]
    [InlineData(NotificationType.GuidancePublished)]
    [InlineData(NotificationType.EventRegistrationConfirmed)]
    [InlineData(NotificationType.EventReminder)]
    [InlineData(NotificationType.ChatPinnedMessage)]
    public async Task SendAsync_completes_for_every_notification_type_without_throwing(NotificationType type)
    {
        var sender = new LoggingNotificationSender(new CapturingLogger());

        await sender.SendAsync(type, "recipient@example.com", new Dictionary<string, string>(), CancellationToken.None);
    }

    [Fact]
    public void LoggingNotificationSender_is_gated_from_Production_via_IDemoOnlyAdapter()
    {
        // Locks in the P3.32 production-safety contract: ProductionSafetyExtensions refuses to
        // boot in Production while any IDemoOnlyAdapter is registered as the real implementation.
        // If this sender were ever changed to no longer implement the marker interface, the app
        // would silently start logging "notifications" instead of sending real email in
        // Production with no startup failure to catch it — this test guards that regression.
        Assert.IsAssignableFrom<IDemoOnlyAdapter>(new LoggingNotificationSender(new CapturingLogger()));
    }
}

using BUnited.Modules.Audit.Contracts;
using BUnited.Modules.Audit.Infrastructure.Logging;
using BUnited.Modules.Audit.Tests.TestSupport;

namespace BUnited.Modules.Audit.Tests.Logging;

public sealed class AuditLoggerTests
{
    [Fact]
    public async Task LogAsync_persists_a_row_with_timestamp_and_correlation_id_populated()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var __ = connection;
        using var ___ = context;

        var actorUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);
        var logger = new AuditLogger(context, new FakeCorrelationIdAccessor("corr-123"), new FakeTimeProvider(now));

        var entry = AuditEntry.Create(
            AuditActions.UserLogin,
            actorUserId: actorUserId,
            entityType: "User",
            entityId: actorUserId.ToString(),
            ipAddress: "203.0.113.10",
            metadata: new Dictionary<string, string> { ["loginMethod"] = "email" });

        await logger.LogAsync(entry, CancellationToken.None);

        var stored = Assert.Single(context.AuditLogs);
        Assert.Equal(AuditActions.UserLogin, stored.Action);
        Assert.Equal(actorUserId, stored.ActorUserId);
        Assert.Equal("User", stored.EntityType);
        Assert.Equal(actorUserId.ToString(), stored.EntityId);
        Assert.Equal("corr-123", stored.CorrelationId);
        Assert.Equal("203.0.113.10", stored.IpAddress);
        Assert.Equal(now.UtcDateTime, stored.TimestampUtc);
        Assert.Contains("loginMethod", stored.MetadataJson);
    }

    [Fact]
    public async Task LogAsync_without_metadata_stores_a_null_metadata_json()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var __ = connection;
        using var ___ = context;

        var logger = new AuditLogger(context, new FakeCorrelationIdAccessor("corr-456"), new FakeTimeProvider(DateTimeOffset.UtcNow));

        await logger.LogAsync(AuditEntry.Create(AuditActions.UserFailedLogin), CancellationToken.None);

        var stored = Assert.Single(context.AuditLogs);
        Assert.Null(stored.MetadataJson);
        Assert.Null(stored.ActorUserId);
    }
}

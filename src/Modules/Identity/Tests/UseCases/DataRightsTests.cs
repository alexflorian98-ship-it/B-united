using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Identity.Application.UseCases.DataRights;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BUnited.Modules.Identity.Tests.UseCases;

/// <summary>P7.04/P7.05/P7.06 — self-service data export and account deletion
/// (docs/DATA_RETENTION_POLICY.md). Cross-module erasure/export participants are exercised via
/// fakes here (Identity's own test DbContext only maps Identity's entities); Billing retention,
/// Progress/Questionnaires/Events erasure, and Chat message-anonymization are additionally
/// live-verified end to end per the task's verification plan.</summary>
public sealed class DataRightsTests
{
    [Fact]
    public async Task DeleteMyAccount_rejects_an_incorrect_password_and_changes_nothing()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var passwordHasher = new PasswordHasher();
        var user = User.Register("ada@example.com", passwordHasher.Hash("correct-horse-battery-staple"));
        user.MarkEmailVerified(DateTime.UtcNow);
        context.Users.Add(user);
        context.UserPreferences.Add(UserPreference.CreateDefault(user.Id));
        context.UserConsents.Add(UserConsent.Record(user.Id, "terms", 1, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var eraser = new RecordingUserDataEraser();
        var auditLogger = new RecordingAuditLogger();
        var handler = new DeleteMyAccountHandler(
            context,
            [eraser],
            passwordHasher,
            new FakeTimeProvider(DateTimeOffset.UtcNow),
            auditLogger,
            NullLogger<DeleteMyAccountHandler>.Instance);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            handler.HandleAsync(new DeleteMyAccountCommand(user.Id, "wrong-password"), CancellationToken.None));

        Assert.Equal("ACCOUNT_DELETION_PASSWORD_INVALID", exception.Code);
        Assert.Empty(eraser.ErasedUserIds);
        Assert.Empty(auditLogger.Entries);

        var reloaded = await context.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        Assert.Equal("ada@example.com", reloaded.Email);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task DeleteMyAccount_with_the_correct_password_anonymizes_the_user_revokes_sessions_and_retains_consent()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var passwordHasher = new PasswordHasher();
        var user = User.Register("ada@example.com", passwordHasher.Hash("correct-horse-battery-staple"));
        user.MarkEmailVerified(DateTime.UtcNow);
        context.Users.Add(user);
        context.UserPreferences.Add(UserPreference.CreateDefault(user.Id));
        context.UserConsents.Add(UserConsent.Record(user.Id, "terms", 1, DateTime.UtcNow));
        context.RefreshTokens.Add(RefreshToken.IssueNew(user.Id, "hash-1", DateTime.UtcNow, DateTime.UtcNow.AddDays(30)));
        await context.SaveChangesAsync();

        var eraser = new RecordingUserDataEraser();
        var auditLogger = new RecordingAuditLogger();
        var handler = new DeleteMyAccountHandler(
            context,
            [eraser],
            passwordHasher,
            new FakeTimeProvider(DateTimeOffset.UtcNow),
            auditLogger,
            NullLogger<DeleteMyAccountHandler>.Instance);

        await handler.HandleAsync(new DeleteMyAccountCommand(user.Id, "correct-horse-battery-staple"), CancellationToken.None);

        // The row survives (UserConsent's Restrict FK forces anonymize-in-place, not hard delete).
        var reloaded = await context.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        Assert.NotEqual("ada@example.com", reloaded.Email);
        Assert.Contains(user.Id.ToString("N"), reloaded.Email);
        Assert.False(reloaded.IsActive);
        Assert.False(passwordHasher.Verify("correct-horse-battery-staple", reloaded.PasswordHash));

        // Sessions revoked (tokens removed).
        Assert.Empty(await context.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync());

        // Preferences dropped, consent retained untouched.
        Assert.Empty(await context.UserPreferences.Where(p => p.UserId == user.Id).ToListAsync());
        var consent = await context.UserConsents.AsNoTracking().SingleAsync(c => c.UserId == user.Id);
        Assert.Equal("terms", consent.ConsentType);

        // Cross-module eraser fanned out to exactly once, for this user.
        Assert.Equal([user.Id], eraser.ErasedUserIds);

        // Metadata-only audit entry written for the deletion itself.
        var entry = Assert.Single(auditLogger.Entries);
        Assert.Equal(BUnited.Modules.Audit.Contracts.AuditActions.UserAccountDeleted, entry.Action);
        Assert.Equal(user.Id, entry.ActorUserId);
    }

    [Fact]
    public async Task DeleteMyAccount_never_touches_a_different_users_data()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var passwordHasher = new PasswordHasher();
        var user = User.Register("ada@example.com", passwordHasher.Hash("password-one"));
        var otherUser = User.Register("bob@example.com", passwordHasher.Hash("password-two"));
        user.MarkEmailVerified(DateTime.UtcNow);
        otherUser.MarkEmailVerified(DateTime.UtcNow);
        context.Users.AddRange(user, otherUser);
        context.UserPreferences.Add(UserPreference.CreateDefault(user.Id));
        context.UserPreferences.Add(UserPreference.CreateDefault(otherUser.Id));
        await context.SaveChangesAsync();

        var handler = new DeleteMyAccountHandler(
            context,
            [],
            passwordHasher,
            new FakeTimeProvider(DateTimeOffset.UtcNow),
            new RecordingAuditLogger(),
            NullLogger<DeleteMyAccountHandler>.Instance);

        await handler.HandleAsync(new DeleteMyAccountCommand(user.Id, "password-one"), CancellationToken.None);

        var otherReloaded = await context.Users.AsNoTracking().SingleAsync(u => u.Id == otherUser.Id);
        Assert.Equal("bob@example.com", otherReloaded.Email);
        Assert.True(otherReloaded.IsActive);
        Assert.NotEmpty(await context.UserPreferences.Where(p => p.UserId == otherUser.Id).ToListAsync());
    }

    [Fact]
    public async Task ExportMyData_aggregates_every_registered_module_section_scoped_to_the_caller()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var exporters = new IUserDataExporter[]
        {
            new FakeUserDataExporter("progress", "progress-data-for"),
            new FakeUserDataExporter("questionnaires", "questionnaire-data-for"),
        };

        var handler = new ExportMyDataHandler(exporters, new FakeTimeProvider(DateTimeOffset.UtcNow));
        var result = await handler.HandleAsync(userId, CancellationToken.None);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(2, result.Sections.Count);
        Assert.Equal("progress-data-for" + userId, result.Sections["progress"]);
        Assert.Equal("questionnaire-data-for" + userId, result.Sections["questionnaires"]);
        Assert.DoesNotContain("progress-data-for" + otherUserId, result.Sections.Values);
    }

    private sealed class RecordingUserDataEraser : IUserDataEraser
    {
        public List<Guid> ErasedUserIds { get; } = [];

        public Task EraseAsync(Guid userId, CancellationToken cancellationToken)
        {
            ErasedUserIds.Add(userId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserDataExporter(string sectionKey, string prefix) : IUserDataExporter
    {
        public string SectionKey => sectionKey;

        public Task<object?> ExportAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<object?>(prefix + userId);
    }
}

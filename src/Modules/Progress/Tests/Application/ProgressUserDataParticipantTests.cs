using BUnited.Modules.Progress.Application.UseCases.DataRights;
using BUnited.Modules.Progress.Domain.Entities;
using BUnited.Modules.Progress.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Progress.Tests.Application;

/// <summary>P7.05/docs/DATA_RETENTION_POLICY.md — Progress hard-deletes a user's own learning
/// history on account deletion.</summary>
public sealed class ProgressUserDataParticipantTests
{
    [Fact]
    public async Task Export_returns_only_the_callers_own_progress()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();

        context.ContentProgressEntries.Add(ContentProgress.Start(userId, contentItemId));
        context.ContentProgressEntries.Add(ContentProgress.Start(otherUserId, contentItemId));
        await context.SaveChangesAsync();

        var exporter = new ProgressUserDataExporter(context);
        var result = await exporter.ExportAsync(userId, CancellationToken.None);

        var json = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.Contains(contentItemId.ToString(), json);
        Assert.DoesNotContain(otherUserId.ToString(), json);
    }

    [Fact]
    public async Task Erase_removes_the_callers_progress_rows_and_leaves_other_users_untouched()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();

        context.ContentProgressEntries.Add(ContentProgress.Start(userId, Guid.NewGuid()));
        context.SectionProgressEntries.Add(SectionProgress.Create(userId, sectionId));
        context.ContentProgressEntries.Add(ContentProgress.Start(otherUserId, Guid.NewGuid()));
        await context.SaveChangesAsync();

        var eraser = new ProgressUserDataEraser(context);
        await eraser.EraseAsync(userId, CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Empty(await context.ContentProgressEntries.Where(p => p.UserId == userId).ToListAsync());
        Assert.Empty(await context.SectionProgressEntries.Where(p => p.UserId == userId).ToListAsync());
        Assert.Single(await context.ContentProgressEntries.Where(p => p.UserId == otherUserId).ToListAsync());
    }
}

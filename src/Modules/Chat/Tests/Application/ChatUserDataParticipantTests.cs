using BUnited.Modules.Chat.Application.UseCases.DataRights;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Chat.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Tests.Application;

/// <summary>P7.05/docs/DATA_RETENTION_POLICY.md/P6.11 — account deletion must never destroy Chat
/// conversation continuity: message rows survive verbatim, only the user's own
/// moderation/read-state bookkeeping is erased.</summary>
public sealed class ChatUserDataParticipantTests
{
    [Fact]
    public async Task Erase_never_touches_message_rows()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var room = ChatRoom.Create(Guid.NewGuid(), "general", "General", null);
        context.ChatRooms.Add(room);
        var userId = Guid.NewGuid();
        var message = Message.Create(room.Id, userId, "hello, still here after I delete my account");
        context.Messages.Add(message);
        context.Mutes.Add(Mute.Create(userId, Guid.NewGuid(), "spam", DateTime.UtcNow.AddDays(1)));
        context.Reports.Add(Report.Create(message.Id, userId, "off-topic"));
        context.ChatReadStates.Add(ChatReadState.Create(userId, room.Id, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var eraser = new ChatUserDataEraser(context);
        await eraser.EraseAsync(userId, CancellationToken.None);
        await context.SaveChangesAsync();

        var reloadedMessage = await context.Messages.AsNoTracking().SingleAsync(m => m.Id == message.Id);
        Assert.Equal("hello, still here after I delete my account", reloadedMessage.Body);
        Assert.Equal(userId, reloadedMessage.UserId);
        Assert.False(reloadedMessage.IsDeleted);

        Assert.Empty(await context.Mutes.Where(m => m.UserId == userId).ToListAsync());
        Assert.Empty(await context.Reports.Where(r => r.ReporterId == userId).ToListAsync());
        Assert.Empty(await context.ChatReadStates.Where(s => s.UserId == userId).ToListAsync());
    }

    [Fact]
    public async Task Export_returns_only_the_callers_own_authored_messages()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var room = ChatRoom.Create(Guid.NewGuid(), "general", "General", null);
        context.ChatRooms.Add(room);
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        context.Messages.Add(Message.Create(room.Id, userId, "mine"));
        context.Messages.Add(Message.Create(room.Id, otherUserId, "not mine"));
        await context.SaveChangesAsync();

        var exporter = new ChatUserDataExporter(context);
        var result = await exporter.ExportAsync(userId, CancellationToken.None);

        var json = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.Contains("mine", json);
        Assert.DoesNotContain("not mine", json);
    }
}

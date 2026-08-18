using BUnited.Modules.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Migrations.Seed;

/// <summary>Real bug found while manually verifying the app after the 2026-08-18 audit fixes:
/// the Community/Chat page rendered with an empty room list and no explanation, because no
/// seeder had ever created a <see cref="ChatRoom"/> for the demo program — `GET /chat/rooms`
/// correctly returned `[]` (ChatController has no bug), there was simply nothing to seed rooms
/// from. Per ChatRoom's own docs, every active room must reference a real, published program and
/// can only be created through an admin-only factory (no client-facing create endpoint) — this
/// mirrors that by seeding directly, the same idempotent pattern <see cref="DemoAccountSeeder"/>
/// uses. Depends on <see cref="DemoProgramSeeder"/> having already run in the same startup pass
/// (skips itself if the demo program isn't there yet).</summary>
public static class DemoChatRoomSeeder
{
    private const string RoomName = "General";

    public static async Task SeedAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var program = await context.Programs.SingleOrDefaultAsync(p => p.Slug == "mindful-living", cancellationToken);
        if (program is null)
        {
            // DemoProgramSeeder hasn't run yet (or was removed) — nothing to attach a room to.
            return;
        }

        // chat_rooms.key has a unique index across the whole table, including the 6 legacy,
        // permanently-deactivated global rooms the ConvertChatRoomToProgramOwnedEntity migration
        // seeded with plain keys like "general" (see that migration) — reusing one of those bare
        // keys here would silently no-op against an inactive, unassociated row instead of
        // creating the active, program-scoped room this demo program actually needs. Scoping the
        // key to the program's own slug keeps it unique and makes a second demo program safe too.
        var roomKey = $"{program.Slug}-general";

        if (await context.Set<ChatRoom>().AnyAsync(r => r.Key == roomKey, cancellationToken))
        {
            return;
        }

        context.Set<ChatRoom>().Add(ChatRoom.Create(program.Id, roomKey, RoomName, createdBy: null));

        await context.SaveChangesAsync(cancellationToken);
    }
}

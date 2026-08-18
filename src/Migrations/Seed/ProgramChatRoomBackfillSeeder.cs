using BUnited.Modules.Chat.Application.UseCases.Provisioning;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Migrations.Seed;

/// <summary>Real bug found while manually verifying the app after the 2026-08-18 audit fixes: the
/// Community/Chat page rendered with an empty room list and no explanation, because no chat room
/// existed for any program. The real, ongoing fix is <see cref="ProgramChatRoomProvisioner"/>,
/// wired into <c>ProgramStatusHandler.PublishAsync</c> (Content module) so every future publish
/// auto-provisions its room, named after the program, with no admin step. This seeder is only the
/// one-time backfill for programs that were published before that wiring existed — it runs the
/// exact same provisioner, so its behavior (idempotent, never resurrects a deliberately
/// deactivated room) is identical to the real, ongoing mechanism.</summary>
public static class ProgramChatRoomBackfillSeeder
{
    public static async Task SeedAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var provisioner = new ProgramChatRoomProvisioner(context);

        var publishedPrograms = await context.Programs
            .Where(p => p.Status == ContentStatus.Published)
            .Select(p => new { p.Id, p.DefaultLanguage })
            .ToListAsync(cancellationToken);

        foreach (var program in publishedPrograms)
        {
            var title = await context.ProgramTranslations
                .Where(t => t.ProgramId == program.Id && t.Language == program.DefaultLanguage)
                .Select(t => t.Title)
                .SingleOrDefaultAsync(cancellationToken);

            if (title is null)
            {
                continue;
            }

            await provisioner.EnsureRoomForProgramAsync(program.Id, title, createdBy: null, cancellationToken);
        }
    }
}

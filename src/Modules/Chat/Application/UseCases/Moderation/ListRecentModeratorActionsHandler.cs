using BUnited.Modules.Chat.Application.Dtos;
using BUnited.Modules.Chat.Domain;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

/// <summary>§53 Recent Moderator Actions screen — a merged, time-ordered view built directly
/// from Chat's own tables (deleted messages, mutes, resolved reports). Not sourced from a
/// generic Audit read-model: no admin-facing Audit read API exists anywhere in this codebase yet
/// (CLAUDE.md — audit reads are a separate, explicitly authorized concern), and Chat already has
/// everything it needs in its own schema without depending on one.</summary>
public sealed class ListRecentModeratorActionsHandler(DbContext dbContext, IUserLookup userLookup)
{
    private const int Limit = 25;

    public async Task<IReadOnlyList<ModeratorActionDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var deletedMessages = await dbContext.Set<Message>().AsNoTracking()
            .Where(m => m.IsDeleted && m.DeletedBy != null)
            .OrderByDescending(m => m.DeletedAtUtc)
            .Take(Limit)
            .ToListAsync(cancellationToken);

        var mutes = await dbContext.Set<Mute>().AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(Limit)
            .ToListAsync(cancellationToken);

        var resolvedReports = await dbContext.Set<Report>().AsNoTracking()
            .Where(r => r.Status != ReportStatus.Open && r.ResolvedBy != null)
            .OrderByDescending(r => r.ResolvedAtUtc)
            .Take(Limit)
            .ToListAsync(cancellationToken);

        var actorIds = deletedMessages.Select(m => m.DeletedBy!.Value)
            .Concat(mutes.Select(m => m.ModeratorId))
            .Concat(resolvedReports.Select(r => r.ResolvedBy!.Value))
            .Concat(mutes.Select(m => m.UserId))
            .Distinct()
            .ToList();
        var users = await userLookup.GetByIdsAsync(actorIds, cancellationToken);

        string? Email(Guid userId) => users.TryGetValue(userId, out var user) ? user.Email : null;

        var actions = new List<ModeratorActionDto>();
        actions.AddRange(deletedMessages.Select(m => new ModeratorActionDto("MessageDeleted", Email(m.DeletedBy!.Value), m.Id.ToString(), m.DeletedAtUtc!.Value)));
        actions.AddRange(mutes.Select(m => new ModeratorActionDto("UserMuted", Email(m.ModeratorId), Email(m.UserId) ?? m.UserId.ToString(), m.CreatedAt)));
        actions.AddRange(resolvedReports.Select(r => new ModeratorActionDto($"Report{r.Status}", Email(r.ResolvedBy!.Value), r.MessageId.ToString(), r.ResolvedAtUtc!.Value)));

        return actions.OrderByDescending(a => a.OccurredAtUtc).Take(Limit).ToList();
    }
}

using BUnited.Modules.Chat.Application.Dtos;
using BUnited.Modules.Chat.Domain;
using BUnited.Modules.Chat.Domain.Entities;
using BUnited.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

/// <summary>§53 Reported Messages screen: message context, author, reporter, reason, timestamp.</summary>
public sealed class ListReportsHandler(DbContext dbContext, IUserLookup userLookup)
{
    public async Task<IReadOnlyList<ReportSummaryDto>> HandleAsync(ReportStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Report>().AsNoTracking().AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
        var messageIds = reports.Select(r => r.MessageId).Distinct().ToList();

        var messages = await dbContext.Set<Message>().AsNoTracking()
            .Where(m => messageIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        var userIds = reports.Select(r => r.ReporterId)
            .Concat(messages.Values.Select(m => m.UserId))
            .Distinct()
            .ToList();
        var users = await userLookup.GetByIdsAsync(userIds, cancellationToken);

        return reports.Select(r =>
        {
            messages.TryGetValue(r.MessageId, out var message);
            var authorEmail = message is not null && users.TryGetValue(message.UserId, out var author) ? author.Email : null;
            var reporterEmail = users.TryGetValue(r.ReporterId, out var reporter) ? reporter.Email : null;

            return new ReportSummaryDto(
                r.Id,
                r.MessageId,
                message?.Body,
                message?.UserId ?? Guid.Empty,
                authorEmail,
                r.ReporterId,
                reporterEmail,
                r.Reason,
                r.Status.ToString(),
                r.CreatedAt);
        }).ToList();
    }
}

using BUnited.BuildingBlocks.Domain;
using BUnited.Modules.Chat.Domain;

namespace BUnited.Modules.Chat.Domain.Entities;

/// <summary>docs/PROMPT.md §33-34/§53 — "message, reporter, reason, timestamp" plus a resolution
/// workflow (Dismiss/Delete Message/Mute User).</summary>
public sealed class Report : IAuditableEntity
{
    private Report()
    {
    }

    public static Report Create(Guid messageId, Guid reporterId, string reason) =>
        new()
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            ReporterId = reporterId,
            Reason = reason,
            Status = ReportStatus.Open,
        };

    public Guid Id { get; private set; }

    public Guid MessageId { get; private set; }

    public Guid ReporterId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public ReportStatus Status { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }

    public Guid? ResolvedBy { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Resolve(ReportStatus resolution, Guid moderatorId, DateTime utcNow)
    {
        if (Status != ReportStatus.Open)
        {
            throw new InvalidOperationException("Only an open report can be resolved.");
        }

        if (resolution == ReportStatus.Open)
        {
            throw new InvalidOperationException("A report cannot be resolved into the Open status.");
        }

        Status = resolution;
        ResolvedAtUtc = utcNow;
        ResolvedBy = moderatorId;
    }
}

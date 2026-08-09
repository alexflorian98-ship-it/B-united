namespace BUnited.Modules.Chat.Application.UseCases.Moderation;

public enum ReportResolutionAction
{
    Dismiss,
    DeleteMessage,
    MuteUser,
}

public sealed record ResolveReportRequest(ReportResolutionAction Action, int MuteDurationMinutes = 60, string? MuteReason = null);

public sealed record ResolveReportCommand(Guid ReportId, Guid ModeratorId, ReportResolutionAction Action, int MuteDurationMinutes, string? MuteReason);

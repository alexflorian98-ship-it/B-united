namespace BUnited.Modules.Chat.Application.UseCases.Client;

public sealed record ReportMessageRequest(string Reason);

public sealed record ReportMessageCommand(Guid MessageId, Guid ReporterId, string Reason);

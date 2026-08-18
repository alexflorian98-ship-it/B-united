using BUnited.Modules.Chat.Contracts;

namespace BUnited.Modules.Content.Tests.TestSupport;

internal sealed class RecordingChatRoomProvisioner : IProgramChatRoomProvisioner
{
    public List<(Guid ProgramId, string ProgramName)> Calls { get; } = [];

    public Task EnsureRoomForProgramAsync(Guid programId, string programName, Guid? createdBy, CancellationToken cancellationToken)
    {
        Calls.Add((programId, programName));
        return Task.CompletedTask;
    }
}

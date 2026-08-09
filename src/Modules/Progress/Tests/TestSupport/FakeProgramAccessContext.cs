using BUnited.BuildingBlocks.Application.Access;

namespace BUnited.Modules.Progress.Tests.TestSupport;

/// <summary>Test double for Billing's cross-module <see cref="IProgramAccessContext"/> — Progress's
/// tests configure ownership directly rather than depending on Billing's real purchase flow
/// (CLAUDE.md: never reference another module's Domain/Infrastructure). Mirrors Content's
/// <c>FakeProgramAccessContext</c>.</summary>
internal sealed class FakeProgramAccessContext : IProgramAccessContext
{
    private readonly HashSet<(Guid UserId, Guid ProgramId)> _owned = [];

    public void GrantAccess(Guid userId, Guid programId) => _owned.Add((userId, programId));

    public Task<bool> HasProgramAccessAsync(Guid userId, Guid programId, CancellationToken cancellationToken) =>
        Task.FromResult(_owned.Contains((userId, programId)));
}

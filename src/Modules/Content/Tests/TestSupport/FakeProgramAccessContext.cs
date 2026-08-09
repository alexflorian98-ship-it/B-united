using BUnited.BuildingBlocks.Application.Access;

namespace BUnited.Modules.Content.Tests.TestSupport;

/// <summary>Test double for Billing's cross-module <see cref="IProgramAccessContext"/> — Content's
/// tests configure ownership directly rather than depending on Billing's real purchase flow
/// (CLAUDE.md: never reference another module's Domain/Infrastructure).</summary>
internal sealed class FakeProgramAccessContext : IProgramAccessContext
{
    private readonly HashSet<(Guid UserId, Guid ProgramId)> _owned = [];

    public void GrantAccess(Guid userId, Guid programId) => _owned.Add((userId, programId));

    public Task<bool> HasProgramAccessAsync(Guid userId, Guid programId, CancellationToken cancellationToken) =>
        Task.FromResult(_owned.Contains((userId, programId)));
}

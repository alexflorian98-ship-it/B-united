using BUnited.Modules.Identity.Contracts;

namespace BUnited.Modules.Audit.Tests.TestSupport;

internal sealed class FakeUserLookup : IUserLookup
{
    public Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<UserSummary?>(new UserSummary(userId, $"{userId}@example.com", DisplayName: null));

    public Task<IReadOnlyDictionary<Guid, UserSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, UserSummary>>(
            userIds.ToDictionary(id => id, id => new UserSummary(id, $"{id}@example.com", DisplayName: null)));
}

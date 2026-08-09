using BUnited.Modules.Identity.Contracts;

namespace BUnited.Modules.Chat.Tests.TestSupport;

internal sealed class FakeUserLookup : IUserLookup
{
    public Dictionary<Guid, UserSummary> Users { get; } = [];

    public Task<UserSummary?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Users.GetValueOrDefault(userId));

    public Task<IReadOnlyDictionary<Guid, UserSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, UserSummary>>(Users.Where(kv => userIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
}

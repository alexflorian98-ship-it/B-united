using BUnited.Modules.Progress.Contracts;

namespace BUnited.Modules.Progress.Tests.TestSupport;

/// <summary>Test double for Content's cross-module <see cref="IContentItemProgramLookup"/> —
/// Progress's tests wire up which content item/section belongs to which program directly, rather
/// than depending on Content's real Domain/Infrastructure (CLAUDE.md).</summary>
internal sealed class FakeContentItemProgramLookup : IContentItemProgramLookup
{
    private readonly Dictionary<Guid, Guid> _programIdByContentItemId = [];
    private readonly Dictionary<Guid, Guid> _programIdBySectionId = [];

    public void MapContentItem(Guid contentItemId, Guid programId) => _programIdByContentItemId[contentItemId] = programId;

    public void MapSection(Guid sectionId, Guid programId) => _programIdBySectionId[sectionId] = programId;

    public Task<Guid?> GetOwningProgramIdForContentItemAsync(Guid contentItemId, CancellationToken cancellationToken) =>
        Task.FromResult(_programIdByContentItemId.TryGetValue(contentItemId, out var programId) ? (Guid?)programId : null);

    public Task<Guid?> GetOwningProgramIdForSectionAsync(Guid sectionId, CancellationToken cancellationToken) =>
        Task.FromResult(_programIdBySectionId.TryGetValue(sectionId, out var programId) ? (Guid?)programId : null);
}

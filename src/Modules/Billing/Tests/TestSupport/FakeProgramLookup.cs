using BUnited.Modules.Content.Contracts;

namespace BUnited.Modules.Billing.Tests.TestSupport;

/// <summary>Test double for Content's cross-module <see cref="IProgramLookup"/> — Billing's tests
/// must never depend on Content's real Domain/Infrastructure (CLAUDE.md), so program
/// existence/publish state is configured directly by the test instead.</summary>
internal sealed class FakeProgramLookup : IProgramLookup
{
    private readonly Dictionary<Guid, ProgramSummary> _programs = [];

    public void AddProgram(Guid programId, ProgramLookupStatus status = ProgramLookupStatus.Published, string slug = "some-program", string defaultLanguage = "ro", string? title = "Some Program") =>
        _programs[programId] = new ProgramSummary(programId, slug, status, defaultLanguage, title);

    public Task<ProgramSummary?> GetProgramAsync(Guid programId, CancellationToken cancellationToken) =>
        Task.FromResult(_programs.GetValueOrDefault(programId));
}

using BUnited.BuildingBlocks.Application.DataRights;

namespace BUnited.Modules.Identity.Application.UseCases.DataRights;

/// <summary>Orchestrates the full-account "export my data" archive (docs/PROMPT.md §66,
/// docs/DATA_RETENTION_POLICY.md) by fan-out over every registered <see cref="IUserDataExporter"/>
/// — one per module that owns user-scoped data (Identity itself, Progress, Questionnaires,
/// Billing, Events, Chat). This handler never references another module's Domain or
/// Infrastructure layer: it only depends on the shared cross-module contract, resolved via DI,
/// mirroring the existing <c>IUserLookup</c>/<c>IProgramLookup</c> pattern.
///
/// Always scoped to the authenticated caller's own <paramref name="userId"/> — there is no
/// admin-supplied identifier path, matching the existing "no implicit administrator access to
/// high-sensitivity personal data" precedent set by the questionnaire export.</summary>
public sealed class ExportMyDataHandler(IEnumerable<IUserDataExporter> exporters, TimeProvider timeProvider)
{
    public async Task<UserDataExportDto> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sections = new Dictionary<string, object?>();

        foreach (var exporter in exporters)
        {
            sections[exporter.SectionKey] = await exporter.ExportAsync(userId, cancellationToken);
        }

        return new UserDataExportDto(userId, timeProvider.GetUtcNow().UtcDateTime, sections);
    }
}

public sealed record UserDataExportDto(Guid UserId, DateTime GeneratedAtUtc, IReadOnlyDictionary<string, object?> Sections);

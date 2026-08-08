namespace BUnited.Modules.Content.Application.Dtos;

/// <summary>Client-facing DTOs never expose <c>translationFallbackUsed</c> (P2.12.b) — the
/// admin editor DTOs (<see cref="ProgramDetailDto"/>) return raw per-language translations
/// instead of a resolved one, so admins can see exactly what's missing.</summary>
public sealed record ClientProgramSummaryDto(
    Guid Id,
    string Slug,
    Guid DomainId,
    string Title,
    string ShortDescription,
    Guid? CoverAssetId,
    int SortOrder);

public sealed record ClientContentItemDto(Guid Id, string Type, int SortOrder, bool IsRequired, string Title, string? Body, Guid? MediaAssetId);

public sealed record ClientSectionDto(Guid Id, int SortOrder, string Title, string Description, IReadOnlyList<ClientContentItemDto> Items);

public sealed record ClientProgramDetailDto(
    Guid Id,
    string Slug,
    Guid DomainId,
    string Title,
    string ShortDescription,
    string Description,
    Guid? CoverAssetId,
    IReadOnlyList<ClientSectionDto> Sections);

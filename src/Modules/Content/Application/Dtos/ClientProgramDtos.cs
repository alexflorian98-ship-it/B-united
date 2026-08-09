namespace BUnited.Modules.Content.Application.Dtos;

/// <summary>Client-facing DTOs never expose <c>translationFallbackUsed</c> (P2.12.b) — the
/// admin editor DTOs (<see cref="ProgramDetailDto"/>) return raw per-language translations
/// instead of a resolved one, so admins can see exactly what's missing.</summary>
public sealed record ClientProgramOfferDto(decimal Amount, string Currency);

/// <summary><see langword="null"/> only for a caller with no resolvable identity (P3.37) — every
/// authenticated caller gets either <c>"Owned"</c> or <c>"NotOwned"</c>, never <see
/// langword="null"/>.</summary>
public static class ClientProgramOwnershipState
{
    public const string Owned = "Owned";
    public const string NotOwned = "NotOwned";
}

public sealed record ClientProgramSummaryDto(
    Guid Id,
    string Slug,
    Guid DomainId,
    string Title,
    string ShortDescription,
    Guid? CoverAssetId,
    int SortOrder,
    ClientProgramOfferDto? ActiveOffer,
    string? OwnershipState);

/// <summary>Question/option TEXT is visible to every caller regardless of ownership — Quiz is not
/// part of the Video/RichText paywall (structure/title is always visible; only paid-only
/// Body/media is stripped there). The domain's <c>QuizOption.IsCorrect</c> MUST NEVER appear here
/// for any caller, ever — it is exposed only after a submission, via
/// <c>SubmitQuizAttemptResult</c>.</summary>
public sealed record ClientQuizOptionDto(Guid Id, int SortOrder, string Label);

public sealed record ClientQuizQuestionDto(Guid Id, int SortOrder, string Text, IReadOnlyList<ClientQuizOptionDto> Options);

public sealed record ClientContentItemDto(
    Guid Id,
    string Type,
    int SortOrder,
    bool IsRequired,
    string Title,
    string? Body,
    Guid? MediaAssetId,
    IReadOnlyList<ClientQuizQuestionDto>? QuizQuestions = null);

public sealed record ClientSectionDto(Guid Id, int SortOrder, string Title, string Description, IReadOnlyList<ClientContentItemDto> Items);

/// <summary><see cref="Sections"/> only ever includes full <c>Body</c>/media content when
/// <see cref="OwnershipState"/> is <see cref="ClientProgramOwnershipState.Owned"/> — this is the
/// actual paywall for the program's content (P3.37.a): every other caller (not owning, or with
/// no resolvable identity at all) sees titles/structure only.</summary>
public sealed record ClientProgramDetailDto(
    Guid Id,
    string Slug,
    Guid DomainId,
    string Title,
    string ShortDescription,
    string Description,
    Guid? CoverAssetId,
    ClientProgramOfferDto? ActiveOffer,
    string? OwnershipState,
    IReadOnlyList<ClientSectionDto> Sections);

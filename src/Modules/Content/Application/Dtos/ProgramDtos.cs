using BUnited.Modules.Content.Domain;

namespace BUnited.Modules.Content.Application.Dtos;

public sealed record ProgramTranslationDto(string Language, string Title, string ShortDescription, string Description);

public sealed record SectionTranslationDto(string Language, string Title, string Description);

public sealed record ContentItemTranslationDto(string Language, string Title, string? Body);

public sealed record AdminQuizOptionTranslationDto(string Language, string Label);

/// <summary>Unlike the client-facing read (<c>ClientQuizOptionDto</c>), the admin view legitimately
/// needs <see cref="IsCorrect"/> to render which option is currently marked correct — this DTO
/// must never be reused on any client-facing (non-admin) endpoint.</summary>
public sealed record AdminQuizOptionDetailDto(
    Guid Id,
    int SortOrder,
    bool IsCorrect,
    IReadOnlyList<AdminQuizOptionTranslationDto> Translations);

public sealed record AdminQuizQuestionTranslationDto(string Language, string Text);

public sealed record AdminQuizQuestionDetailDto(
    Guid Id,
    int SortOrder,
    IReadOnlyList<AdminQuizQuestionTranslationDto> Translations,
    IReadOnlyList<AdminQuizOptionDetailDto> Options);

public sealed record ContentItemDetailDto(
    Guid Id,
    ContentItemType Type,
    int SortOrder,
    bool IsRequired,
    Guid? MediaAssetId,
    IReadOnlyList<ContentItemTranslationDto> Translations,
    IReadOnlyList<AdminQuizQuestionDetailDto>? QuizQuestions = null);

public sealed record SectionDetailDto(
    Guid Id,
    int SortOrder,
    ContentStatus Status,
    IReadOnlyList<SectionTranslationDto> Translations,
    IReadOnlyList<ContentItemDetailDto> Items);

public sealed record ProgramSummaryDto(
    Guid Id,
    string Slug,
    ContentStatus Status,
    string DefaultLanguage,
    Guid DomainId,
    int SortOrder,
    string Title,
    int SectionCount,
    IReadOnlyList<string> Languages,
    DateTime UpdatedAt);

public sealed record ProgramListResult(IReadOnlyList<ProgramSummaryDto> Items, int TotalCount);

public sealed record ProgramDetailDto(
    Guid Id,
    Guid DomainId,
    string Slug,
    ContentStatus Status,
    string DefaultLanguage,
    Guid? CoverAssetId,
    int SortOrder,
    IReadOnlyList<ProgramTranslationDto> Translations,
    IReadOnlyList<SectionDetailDto> Sections);

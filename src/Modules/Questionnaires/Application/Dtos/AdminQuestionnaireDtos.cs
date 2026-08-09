namespace BUnited.Modules.Questionnaires.Application.Dtos;

public sealed record QuestionnaireTranslationDto(string Language, string Title, string Description);

public sealed record QuestionOptionTranslationDto(string Language, string Label);

public sealed record QuestionOptionDetailDto(
    Guid Id,
    string Value,
    int SortOrder,
    IReadOnlyList<QuestionOptionTranslationDto> Translations);

public sealed record QuestionTranslationDto(string Language, string Text, string? HelpText);

public sealed record QuestionDetailDto(
    Guid Id,
    string Type,
    int SortOrder,
    bool IsRequired,
    IReadOnlyList<QuestionTranslationDto> Translations,
    IReadOnlyList<QuestionOptionDetailDto> Options);

public sealed record QuestionnaireDetailDto(
    Guid Id,
    Guid ProgramId,
    string Status,
    string DefaultLanguage,
    IReadOnlyList<QuestionnaireTranslationDto> Translations,
    IReadOnlyList<QuestionDetailDto> Questions);

public sealed record QuestionnaireSummaryDto(
    Guid Id,
    Guid ProgramId,
    string Status,
    string DefaultLanguage,
    string Title,
    int QuestionCount,
    IReadOnlyList<string> Languages,
    DateTime UpdatedAt);

public sealed record QuestionnaireListResult(IReadOnlyList<QuestionnaireSummaryDto> Items, int TotalCount);

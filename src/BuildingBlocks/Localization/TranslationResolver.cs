namespace BUnited.BuildingBlocks.Localization;

/// <summary>
/// Resolves DB-backed content translations (Program, Section, ContentItem, Event, Question,
/// QuestionOption, Questionnaire — docs/ARCHITECTURE.md §16): exact language match if present,
/// otherwise the parent entity's default-language row, flagged via
/// <see cref="TranslationResolution{TTranslation}.FallbackUsed"/>.
/// </summary>
public static class TranslationResolver
{
    public static TranslationResolution<TTranslation> Resolve<TTranslation>(
        IEnumerable<TTranslation> translations,
        string requestedLanguage,
        string defaultLanguage)
        where TTranslation : class, ITranslation
    {
        var materialized = translations as IReadOnlyCollection<TTranslation> ?? translations.ToList();

        var exactMatch = materialized.FirstOrDefault(
            t => string.Equals(t.Language, requestedLanguage, StringComparison.OrdinalIgnoreCase));
        if (exactMatch is not null)
        {
            return new TranslationResolution<TTranslation>(exactMatch, FallbackUsed: false);
        }

        var defaultLanguageMatch = materialized.FirstOrDefault(
            t => string.Equals(t.Language, defaultLanguage, StringComparison.OrdinalIgnoreCase));
        if (defaultLanguageMatch is not null)
        {
            return new TranslationResolution<TTranslation>(defaultLanguageMatch, FallbackUsed: true);
        }

        throw new InvalidOperationException(
            $"No translation found for the requested language ('{requestedLanguage}') or the " +
            $"entity's default language ('{defaultLanguage}') among {materialized.Count} " +
            $"translation(s). Every translatable entity must have at least a default-language " +
            "translation row — this indicates a data-integrity bug, not a normal missing-translation case.");
    }
}

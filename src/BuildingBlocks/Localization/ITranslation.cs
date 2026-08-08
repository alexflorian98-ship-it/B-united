namespace BUnited.BuildingBlocks.Localization;

/// <summary>
/// Implemented by every `*Translation` entity (e.g. `ProgramTranslation`,
/// `QuestionnaireTranslation`) so <see cref="TranslationResolver"/> can pick the right row
/// without knowing about entity-specific fields.
/// </summary>
public interface ITranslation
{
    string Language { get; }
}

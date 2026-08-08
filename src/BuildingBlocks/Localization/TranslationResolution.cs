namespace BUnited.BuildingBlocks.Localization;

/// <summary>
/// The result of resolving a translation for a requested language. <see cref="FallbackUsed"/>
/// is exposed only in admin DTOs — client-facing DTOs must fall back silently
/// (docs/ARCHITECTURE.md §16).
/// </summary>
public sealed record TranslationResolution<TTranslation>(TTranslation Translation, bool FallbackUsed)
    where TTranslation : class, ITranslation;

using BUnited.BuildingBlocks.Localization;

namespace BUnited.BuildingBlocks.Localization.Tests;

public sealed class TranslationResolverTests
{
    private sealed record SampleTranslation(string Language, string Title) : ITranslation;

    [Fact]
    public void Returns_the_exact_language_match_without_fallback()
    {
        SampleTranslation[] translations =
        [
            new("ro", "Titlu RO"),
            new("en", "Title EN"),
        ];

        var result = TranslationResolver.Resolve(translations, requestedLanguage: "en", defaultLanguage: "ro");

        Assert.Equal("Title EN", result.Translation.Title);
        Assert.False(result.FallbackUsed);
    }

    [Fact]
    public void Falls_back_to_the_default_language_when_the_requested_one_is_missing()
    {
        SampleTranslation[] translations = [new("ro", "Titlu RO")];

        var result = TranslationResolver.Resolve(translations, requestedLanguage: "en", defaultLanguage: "ro");

        Assert.Equal("Titlu RO", result.Translation.Title);
        Assert.True(result.FallbackUsed);
    }

    [Fact]
    public void Language_matching_is_case_insensitive()
    {
        SampleTranslation[] translations = [new("RO", "Titlu RO")];

        var result = TranslationResolver.Resolve(translations, requestedLanguage: "ro", defaultLanguage: "ro");

        Assert.False(result.FallbackUsed);
    }

    [Fact]
    public void Throws_when_neither_the_requested_nor_the_default_language_exists()
    {
        SampleTranslation[] translations = [new("fr", "Titre FR")];

        var exception = Assert.Throws<InvalidOperationException>(
            () => TranslationResolver.Resolve(translations, requestedLanguage: "en", defaultLanguage: "ro"));

        Assert.Contains("data-integrity", exception.Message);
    }

    [Fact]
    public void Throws_for_an_empty_translation_collection()
    {
        Assert.Throws<InvalidOperationException>(
            () => TranslationResolver.Resolve(
                Array.Empty<SampleTranslation>(),
                requestedLanguage: "en",
                defaultLanguage: "ro"));
    }
}

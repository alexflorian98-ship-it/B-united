using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Migrations.Seed;

/// <summary>docs/TASKS.md P2.30 — one permanent, fully translated (ro+en) demo program, so a
/// fresh environment always has real content to browse/buy/consume without an admin authoring
/// anything first. Idempotent, same guard style as <see cref="ContentSeeder"/>: keyed off a
/// fixed slug rather than a well-known GUID, since <see cref="Program.Slug"/> is the module's own
/// natural uniqueness key for an admin-authored-looking entity.</summary>
public static class DemoProgramSeeder
{
    private const string Slug = "mindful-living";

    public static async Task SeedAsync(BUnitedApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Programs.AnyAsync(p => p.Slug == Slug, cancellationToken))
        {
            return;
        }

        var program = Program.Create(WellKnownContentDomains.All.Single(d => d.Slug == WellKnownContentDomains.Psychology).Id, Slug, defaultLanguage: "ro", createdBy: null);
        context.Programs.Add(program);
        context.ProgramTranslations.Add(ProgramTranslation.Create(
            program.Id, "ro",
            "Trai constient",
            "Un ghid practic pentru a-ti calma mintea si a-ti reconstrui atentia, zi de zi.",
            "Acest program iti ofera un set de exercitii ghidate si lecturi scurte pentru a introduce mindfulness-ul in rutina zilnica. Nu e nevoie de experienta anterioara — doar cateva minute pe zi, constant."));
        context.ProgramTranslations.Add(ProgramTranslation.Create(
            program.Id, "en",
            "Mindful Living",
            "A practical guide to calming your mind and rebuilding your attention, day by day.",
            "This program gives you a set of guided exercises and short readings to bring mindfulness into your daily routine. No prior experience needed — just a few consistent minutes a day."));

        var introSection = Section.Create(program.Id, sortOrder: 0);
        context.Sections.Add(introSection);
        context.SectionTranslations.Add(SectionTranslation.Create(introSection.Id, "ro", "Introducere", "Primii pasi catre o practica zilnica de mindfulness."));
        context.SectionTranslations.Add(SectionTranslation.Create(introSection.Id, "en", "Getting started", "Your first steps toward a daily mindfulness practice."));

        // A real, freely licensed (Creative Commons) YouTube video — registered synchronously per
        // ADR-005, mirroring exactly how AddContentItemHandler registers an expert-pasted URL.
        var mediaAsset = MediaAsset.Create("youtube", "aqz-KE-bpKQ");
        mediaAsset.MarkReady(providerPlaybackId: "aqz-KE-bpKQ", durationSeconds: null, thumbnailUrl: "https://img.youtube.com/vi/aqz-KE-bpKQ/hqdefault.jpg");
        context.MediaAssets.Add(mediaAsset);

        var videoItem = ContentItem.Create(introSection.Id, ContentItemType.Video, sortOrder: 0, isRequired: true, mediaAsset.Id);
        context.ContentItems.Add(videoItem);
        context.ContentItemTranslations.Add(ContentItemTranslation.Create(videoItem.Id, "ro", "Bun venit in program", null));
        context.ContentItemTranslations.Add(ContentItemTranslation.Create(videoItem.Id, "en", "Welcome to the program", null));

        var richTextItem = ContentItem.Create(introSection.Id, ContentItemType.RichText, sortOrder: 1, isRequired: true, mediaAssetId: null);
        context.ContentItems.Add(richTextItem);
        context.ContentItemTranslations.Add(ContentItemTranslation.Create(
            richTextItem.Id, "ro", "De ce conteaza mindfulness-ul",
            "<p>Mindfulness-ul inseamna sa fii prezent, cu intentie, fara sa judeci ceea ce observi. " +
            "Cercetarile arata ca o practica scurta si constanta poate reduce stresul si imbunatati concentrarea.</p>" +
            "<p>Marcheaza aceasta lectie ca finalizata cand ai citit-o, apoi continua cu urmatorul exercitiu.</p>"));
        context.ContentItemTranslations.Add(ContentItemTranslation.Create(
            richTextItem.Id, "en", "Why mindfulness matters",
            "<p>Mindfulness means being present, on purpose, without judging what you notice. " +
            "Research shows that a short, consistent practice can reduce stress and improve focus.</p>" +
            "<p>Mark this lesson as completed once you have read it, then continue to the next exercise.</p>"));

        var practiceSection = Section.Create(program.Id, sortOrder: 1);
        context.Sections.Add(practiceSection);
        context.SectionTranslations.Add(SectionTranslation.Create(practiceSection.Id, "ro", "Practica ghidata", "Exercitii scurte pe care le poti face oriunde."));
        context.SectionTranslations.Add(SectionTranslation.Create(practiceSection.Id, "en", "Guided practice", "Short exercises you can do anywhere."));

        var practiceRichTextItem = ContentItem.Create(practiceSection.Id, ContentItemType.RichText, sortOrder: 0, isRequired: false, mediaAssetId: null);
        context.ContentItems.Add(practiceRichTextItem);
        context.ContentItemTranslations.Add(ContentItemTranslation.Create(
            practiceRichTextItem.Id, "ro", "Exercitiul de respiratie de 3 minute",
            "<p>Aseaza-te confortabil. Timp de un minut, observa-ti respiratia fara sa o schimbi. " +
            "Timp de un minut, numara fiecare expiratie pana la 10, apoi ia-o de la capat. " +
            "In ultimul minut, largeste-ti atentia catre sunetele din jur.</p>"));
        context.ContentItemTranslations.Add(ContentItemTranslation.Create(
            practiceRichTextItem.Id, "en", "The 3-minute breathing exercise",
            "<p>Sit comfortably. For one minute, notice your breath without changing it. " +
            "For one minute, count each exhale up to 10, then start over. " +
            "In the final minute, widen your attention to the sounds around you.</p>"));

        await context.SaveChangesAsync(cancellationToken);

        // Publish after the initial save so every FK the publish gate might eventually check
        // (sections/items) already exists — mirrors the admin authoring flow's own ordering.
        program.Publish(updatedBy: Guid.Empty);
        introSection.Publish();
        practiceSection.Publish();
        await context.SaveChangesAsync(cancellationToken);
    }
}

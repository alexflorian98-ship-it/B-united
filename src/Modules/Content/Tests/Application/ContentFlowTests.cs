using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;
using BUnited.Modules.Content.Application.UseCases.Admin.Programs;
using BUnited.Modules.Content.Application.UseCases.Admin.Sections;
using BUnited.Modules.Content.Application.UseCases.Client;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using BUnited.Modules.Content.Infrastructure.Video;
using BUnited.Modules.Content.Tests.TestSupport;

namespace BUnited.Modules.Content.Tests.Application;

/// <summary>
/// Chains the real handlers together the way the admin authoring UI + client reads actually
/// would — create → translate → add sections/items → publish → client-visible with translation
/// fallback — proving the wiring, not re-testing already-covered pieces (<c>TranslationResolver</c>
/// itself has its own tests in BuildingBlocks.Localization.Tests; P2.31's requirement is
/// satisfied by that plus this handler-level wiring proof).
/// </summary>
public sealed class ContentFlowTests
{
    private static readonly Guid ActorId = Guid.NewGuid();

    private static async Task<(TestSupport.TestDbContext Context, Microsoft.Data.Sqlite.SqliteConnection Connection, Guid DomainId)> SeedAsync()
    {
        var (connection, context) = TestDbContextFactory.Create();
        var domain = ContentDomain.Create(Guid.NewGuid(), "psychology", 1);
        context.ContentDomains.Add(domain);
        await context.SaveChangesAsync();
        return (context, connection, domain.Id);
    }

    [Fact]
    public async Task Draft_program_is_invisible_to_clients_but_visible_after_publish_with_translation_fallback()
    {
        var (context, connection, domainId) = await SeedAsync();
        using var _ = connection;
        using var __ = context;

        var createHandler = new CreateProgramHandler(context);
        var programId = await createHandler.HandleAsync(
            new CreateProgramCommand(domainId, "managing-anxiety", "ro", "Gestionarea anxietatii", "Scurt", "Descriere completa", ActorId),
            CancellationToken.None);

        var listHandler = new ListPublishedProgramsHandler(context);
        var beforePublish = await listHandler.HandleAsync(new ListPublishedProgramsQuery(null, "ro"), CancellationToken.None);
        Assert.Empty(beforePublish);

        var statusHandler = new ProgramStatusHandler(context);
        await statusHandler.PublishAsync(programId, ActorId, CancellationToken.None);

        var afterPublishRo = await listHandler.HandleAsync(new ListPublishedProgramsQuery(null, "ro"), CancellationToken.None);
        var program = Assert.Single(afterPublishRo);
        Assert.Equal("Gestionarea anxietatii", program.Title);

        // No English translation was ever added — requesting "en" must fall back to the
        // program's default language ("ro") rather than erroring or omitting the program.
        var afterPublishEn = await listHandler.HandleAsync(new ListPublishedProgramsQuery(null, "en"), CancellationToken.None);
        Assert.Equal("Gestionarea anxietatii", Assert.Single(afterPublishEn).Title);
    }

    [Fact]
    public async Task Full_authoring_flow_produces_a_correct_client_detail_view()
    {
        var (context, connection, domainId) = await SeedAsync();
        using var _ = connection;
        using var __ = context;

        var programId = await new CreateProgramHandler(context).HandleAsync(
            new CreateProgramCommand(domainId, "managing-anxiety", "ro", "Titlu RO", "Scurt RO", "Descriere RO", ActorId),
            CancellationToken.None);

        await new UpsertProgramTranslationHandler(context).HandleAsync(
            new UpsertProgramTranslationCommand(programId, "en", "Title EN", "Short EN", "Description EN"), CancellationToken.None);

        var sectionId = await new AddSectionHandler(context).HandleAsync(
            new AddSectionCommand(programId, "ro", "Introducere", "Descriere sectiune"), CancellationToken.None);

        var videoProvider = new YouTubeVideoProvider();
        var addItemHandler = new AddContentItemHandler(context, videoProvider);

        await addItemHandler.HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.Video, true, "ro", "Video intro", null, "https://www.youtube.com/watch?v=dQw4w9WgXcQ"),
            CancellationToken.None);
        await addItemHandler.HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.RichText, true, "ro", "Notite", "<p>Continut</p>", null),
            CancellationToken.None);

        await new ProgramStatusHandler(context).PublishAsync(programId, ActorId, CancellationToken.None);

        var detail = await new GetPublishedProgramDetailHandler(context).HandleAsync("managing-anxiety", "en", CancellationToken.None);

        Assert.Equal("Title EN", detail.Title);
        var section = Assert.Single(detail.Sections);
        // Section/item have no "en" translation — must fall back to the program's default ("ro").
        Assert.Equal("Introducere", section.Title);
        Assert.Equal(2, section.Items.Count);
        Assert.Contains(section.Items, i => i.Type == "Video" && i.MediaAssetId is not null);
        Assert.Contains(section.Items, i => i.Type == "RichText" && i.Body == "<p>Continut</p>");
    }

    [Fact]
    public async Task Reordering_content_items_with_the_wrong_id_set_is_rejected()
    {
        var (context, connection, domainId) = await SeedAsync();
        using var _ = connection;
        using var __ = context;

        var programId = await new CreateProgramHandler(context).HandleAsync(
            new CreateProgramCommand(domainId, "prog", "ro", "T", "S", "D", ActorId), CancellationToken.None);
        var sectionId = await new AddSectionHandler(context).HandleAsync(
            new AddSectionCommand(programId, "ro", "Sectiune", "D"), CancellationToken.None);

        var addItemHandler = new AddContentItemHandler(context, new YouTubeVideoProvider());
        var item1 = await addItemHandler.HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.RichText, true, "ro", "Item 1", "<p>1</p>", null), CancellationToken.None);
        await addItemHandler.HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.RichText, true, "ro", "Item 2", "<p>2</p>", null), CancellationToken.None);

        var reorderHandler = new ReorderContentItemsHandler(context);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => reorderHandler.HandleAsync(new ReorderContentItemsCommand(sectionId, [item1]), CancellationToken.None));

        Assert.Equal("CONTENT_ITEM_REORDER_SET_MISMATCH", exception.Code);
    }

    [Fact]
    public async Task Publishing_an_archived_program_is_rejected()
    {
        var (context, connection, domainId) = await SeedAsync();
        using var _ = connection;
        using var __ = context;

        var programId = await new CreateProgramHandler(context).HandleAsync(
            new CreateProgramCommand(domainId, "prog", "ro", "T", "S", "D", ActorId), CancellationToken.None);

        var statusHandler = new ProgramStatusHandler(context);
        await statusHandler.ArchiveAsync(programId, ActorId, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => statusHandler.PublishAsync(programId, ActorId, CancellationToken.None));

        Assert.Equal("PROGRAM_STATUS_TRANSITION_INVALID", exception.Code);
    }

    [Fact]
    public async Task An_invalid_video_reference_is_rejected_with_a_business_rule_error()
    {
        var (context, connection, domainId) = await SeedAsync();
        using var _ = connection;
        using var __ = context;

        var programId = await new CreateProgramHandler(context).HandleAsync(
            new CreateProgramCommand(domainId, "prog", "ro", "T", "S", "D", ActorId), CancellationToken.None);
        var sectionId = await new AddSectionHandler(context).HandleAsync(
            new AddSectionCommand(programId, "ro", "Sectiune", "D"), CancellationToken.None);

        var addItemHandler = new AddContentItemHandler(context, new YouTubeVideoProvider());

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(() => addItemHandler.HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.Video, true, "ro", "Video", null, "not-a-youtube-url"),
            CancellationToken.None));

        Assert.Equal("VIDEO_REFERENCE_INVALID", exception.Code);
    }
}

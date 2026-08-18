using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Audit.Contracts;
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

        var listHandler = new ListPublishedProgramsHandler(context, new FakeProgramOfferLookup(), new FakeProgramAccessContext());
        var beforePublish = await listHandler.HandleAsync(new ListPublishedProgramsQuery(null, "ro", null), CancellationToken.None);
        Assert.Empty(beforePublish);

        var auditLogger = new FakeAuditLogger();
        var chatRoomProvisioner = new RecordingChatRoomProvisioner();
        var statusHandler = new ProgramStatusHandler(context, auditLogger, chatRoomProvisioner);
        await statusHandler.PublishAsync(programId, ActorId, CancellationToken.None);

        Assert.Contains(auditLogger.Entries, e => e.Action == "content.published" && e.EntityId == programId.ToString());

        // Publishing must provision exactly one chat room request, named after the program's own
        // default-language title — no separate manual admin step (product decision, 2026-08-18).
        var provisionCall = Assert.Single(chatRoomProvisioner.Calls);
        Assert.Equal(programId, provisionCall.ProgramId);
        Assert.Equal("Gestionarea anxietatii", provisionCall.ProgramName);

        var afterPublishRo = await listHandler.HandleAsync(new ListPublishedProgramsQuery(null, "ro", null), CancellationToken.None);
        var program = Assert.Single(afterPublishRo);
        Assert.Equal("Gestionarea anxietatii", program.Title);

        // No English translation was ever added — requesting "en" must fall back to the
        // program's default language ("ro") rather than erroring or omitting the program.
        var afterPublishEn = await listHandler.HandleAsync(new ListPublishedProgramsQuery(null, "en", null), CancellationToken.None);
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

        await new ProgramStatusHandler(context, new FakeAuditLogger(), new RecordingChatRoomProvisioner()).PublishAsync(programId, ActorId, CancellationToken.None);

        var userId = Guid.NewGuid();
        var accessContext = new FakeProgramAccessContext();
        accessContext.GrantAccess(userId, programId);
        var detailHandler = new GetPublishedProgramDetailHandler(context, new FakeProgramOfferLookup(), accessContext);

        var detail = await detailHandler.HandleAsync("managing-anxiety", "en", userId, CancellationToken.None);

        Assert.Equal("Title EN", detail.Title);
        Assert.Equal("Owned", detail.OwnershipState);
        var section = Assert.Single(detail.Sections);
        // Section/item have no "en" translation — must fall back to the program's default ("ro").
        Assert.Equal("Introducere", section.Title);
        Assert.Equal(2, section.Items.Count);
        Assert.Contains(section.Items, i => i.Type == "Video" && i.MediaAssetId is not null);
        Assert.Contains(section.Items, i => i.Type == "RichText" && i.Body == "<p>Continut</p>");
    }

    [Fact]
    public async Task Non_owning_and_anonymous_callers_never_see_body_or_media_content()
    {
        var (context, connection, domainId) = await SeedAsync();
        using var _ = connection;
        using var __ = context;

        var programId = await new CreateProgramHandler(context).HandleAsync(
            new CreateProgramCommand(domainId, "managing-anxiety", "ro", "Titlu RO", "Scurt RO", "Descriere RO", ActorId),
            CancellationToken.None);

        var sectionId = await new AddSectionHandler(context).HandleAsync(
            new AddSectionCommand(programId, "ro", "Introducere", "Descriere sectiune"), CancellationToken.None);

        var addItemHandler = new AddContentItemHandler(context, new YouTubeVideoProvider());
        await addItemHandler.HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.Video, true, "ro", "Video intro", null, "https://www.youtube.com/watch?v=dQw4w9WgXcQ"),
            CancellationToken.None);
        await addItemHandler.HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.RichText, true, "ro", "Notite", "<p>Continut</p>", null),
            CancellationToken.None);

        await new ProgramStatusHandler(context, new FakeAuditLogger(), new RecordingChatRoomProvisioner()).PublishAsync(programId, ActorId, CancellationToken.None);

        var offerLookup = new FakeProgramOfferLookup();
        offerLookup.SetActiveOffer(programId, 199.00m, "RON");
        var nonOwningAccessContext = new FakeProgramAccessContext();
        var detailHandler = new GetPublishedProgramDetailHandler(context, offerLookup, nonOwningAccessContext);

        // A signed-in caller who does not own the program.
        var nonOwnerId = Guid.NewGuid();
        var nonOwnerDetail = await detailHandler.HandleAsync("managing-anxiety", "ro", nonOwnerId, CancellationToken.None);
        Assert.Equal("NotOwned", nonOwnerDetail.OwnershipState);
        Assert.All(nonOwnerDetail.Sections.SelectMany(s => s.Items), i => Assert.Null(i.Body));
        Assert.All(nonOwnerDetail.Sections.SelectMany(s => s.Items), i => Assert.Null(i.MediaAssetId));
        Assert.NotNull(nonOwnerDetail.ActiveOffer);
        Assert.Equal(199.00m, nonOwnerDetail.ActiveOffer!.Amount);

        // A caller with no resolvable identity at all.
        var anonymousDetail = await detailHandler.HandleAsync("managing-anxiety", "ro", null, CancellationToken.None);
        Assert.Null(anonymousDetail.OwnershipState);
        Assert.All(anonymousDetail.Sections.SelectMany(s => s.Items), i => Assert.Null(i.Body));
        Assert.All(anonymousDetail.Sections.SelectMany(s => s.Items), i => Assert.Null(i.MediaAssetId));

        // Structure (titles, IsRequired, item type, section titles) is still fully visible to
        // everyone — only body/media content is paywalled.
        Assert.Equal(2, nonOwnerDetail.Sections.Single().Items.Count);
        Assert.Contains(nonOwnerDetail.Sections.Single().Items, i => i.Type == "Video" && i.Title == "Video intro");
        Assert.Contains(nonOwnerDetail.Sections.Single().Items, i => i.Type == "RichText" && i.IsRequired);
    }

    [Fact]
    public async Task Video_playback_requires_access_to_the_owning_program_not_just_any_program()
    {
        var (context, connection, domainId) = await SeedAsync();
        using var _ = connection;
        using var __ = context;

        var ownedProgramId = await new CreateProgramHandler(context).HandleAsync(
            new CreateProgramCommand(domainId, "owned-program", "ro", "Deținut", "S", "D", ActorId), CancellationToken.None);
        var otherProgramId = await new CreateProgramHandler(context).HandleAsync(
            new CreateProgramCommand(domainId, "other-program", "ro", "Alt program", "S", "D", ActorId), CancellationToken.None);

        var sectionId = await new AddSectionHandler(context).HandleAsync(
            new AddSectionCommand(ownedProgramId, "ro", "Introducere", "D"), CancellationToken.None);
        var contentItemId = await new AddContentItemHandler(context, new YouTubeVideoProvider()).HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.Video, true, "ro", "Video", null, "https://www.youtube.com/watch?v=dQw4w9WgXcQ"),
            CancellationToken.None);

        var userId = Guid.NewGuid();
        var accessContext = new FakeProgramAccessContext();
        var playbackHandler = new GetVideoPlaybackHandler(context, accessContext, new YouTubeVideoProvider());

        // Owns a different program entirely — must still be denied for this content item.
        accessContext.GrantAccess(userId, otherProgramId);
        var deniedForOwnerOfOtherProgram = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => playbackHandler.HandleAsync(contentItemId, userId, CancellationToken.None));
        Assert.Equal(ProgramAccessErrorCodes.ProgramAccessRequired, deniedForOwnerOfOtherProgram.Code);

        // Owns no program at all.
        var strangerId = Guid.NewGuid();
        await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => playbackHandler.HandleAsync(contentItemId, strangerId, CancellationToken.None));

        // Owns the actual owning program — succeeds.
        accessContext.GrantAccess(userId, ownedProgramId);
        var result = await playbackHandler.HandleAsync(contentItemId, userId, CancellationToken.None);
        Assert.Contains("youtube.com/embed/", result.PlaybackUrl);
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

        var statusHandler = new ProgramStatusHandler(context, new FakeAuditLogger(), new RecordingChatRoomProvisioner());
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

using BUnited.Modules.Questionnaires.Domain.Entities;

namespace BUnited.Modules.Questionnaires.Tests.Domain;

public sealed class GuidanceResponseTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_draft_has_no_published_at()
    {
        var guidance = GuidanceResponse.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), version: 1, body: "Draft body");
        Assert.Null(guidance.PublishedAt);
    }

    [Fact]
    public void Draft_body_can_be_edited_before_publish()
    {
        var guidance = GuidanceResponse.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), version: 1, body: "First draft");
        guidance.UpdateDraftBody("Revised draft");
        Assert.Equal("Revised draft", guidance.Body);
    }

    [Fact]
    public void Publish_sets_published_at()
    {
        var guidance = GuidanceResponse.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), version: 1, body: "Body");
        guidance.Publish(UtcNow);
        Assert.Equal(UtcNow, guidance.PublishedAt);
    }

    [Fact]
    public void Published_guidance_body_is_immutable()
    {
        var guidance = GuidanceResponse.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), version: 1, body: "Body");
        guidance.Publish(UtcNow);

        Assert.Throws<InvalidOperationException>(() => guidance.UpdateDraftBody("Trying to silently overwrite"));
    }

    [Fact]
    public void Publishing_twice_is_rejected()
    {
        var guidance = GuidanceResponse.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), version: 1, body: "Body");
        guidance.Publish(UtcNow);

        Assert.Throws<InvalidOperationException>(() => guidance.Publish(UtcNow.AddMinutes(1)));
    }
}

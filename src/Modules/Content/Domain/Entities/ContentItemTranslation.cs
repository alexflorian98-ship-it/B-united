using BUnited.BuildingBlocks.Localization;

namespace BUnited.Modules.Content.Domain.Entities;

/// <summary><see cref="Body"/>'s meaning depends on the parent <see cref="ContentItem.Type"/> —
/// rich text for <see cref="ContentItemType.RichText"/>, unused (null) for
/// <see cref="ContentItemType.Video"/> (docs/PROMPT.md §18–22).</summary>
public sealed class ContentItemTranslation : ITranslation
{
    private ContentItemTranslation()
    {
    }

    public static ContentItemTranslation Create(Guid contentItemId, string language, string title, string? body) =>
        new()
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            Language = language,
            Title = title,
            Body = body,
        };

    public Guid Id { get; private set; }

    public Guid ContentItemId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Body { get; private set; }

    public void Update(string title, string? body)
    {
        Title = title;
        Body = body;
    }
}

using BUnited.BuildingBlocks.Localization;

namespace BUnited.Modules.Content.Domain.Entities;

public sealed class SectionTranslation : ITranslation
{
    private SectionTranslation()
    {
    }

    public static SectionTranslation Create(Guid sectionId, string language, string title, string description) =>
        new()
        {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            Language = language,
            Title = title,
            Description = description,
        };

    public Guid Id { get; private set; }

    public Guid SectionId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public void Update(string title, string description)
    {
        Title = title;
        Description = description;
    }
}

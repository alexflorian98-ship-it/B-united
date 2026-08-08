using BUnited.BuildingBlocks.Localization;

namespace BUnited.Modules.Content.Domain.Entities;

public sealed class ProgramTranslation : ITranslation
{
    private ProgramTranslation()
    {
    }

    public static ProgramTranslation Create(Guid programId, string language, string title, string shortDescription, string description) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProgramId = programId,
            Language = language,
            Title = title,
            ShortDescription = shortDescription,
            Description = description,
        };

    public Guid Id { get; private set; }

    public Guid ProgramId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string ShortDescription { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public void Update(string title, string shortDescription, string description)
    {
        Title = title;
        ShortDescription = shortDescription;
        Description = description;
    }
}

using BUnited.BuildingBlocks.Localization;

namespace BUnited.Modules.Events.Domain.Entities;

public sealed class EventTranslation : ITranslation
{
    private EventTranslation()
    {
    }

    public static EventTranslation Create(Guid eventId, string language, string title, string description) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Language = language,
            Title = title,
            Description = description,
        };

    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public string Language { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public void Update(string title, string description)
    {
        Title = title;
        Description = description;
    }
}

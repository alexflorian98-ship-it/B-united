using BUnited.Modules.Content.Domain;

namespace BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;

public sealed record AddContentItemRequest(
    ContentItemType Type,
    bool IsRequired,
    string Language,
    string Title,
    string? Body,
    string? VideoReference);

public sealed record AddContentItemCommand(
    Guid SectionId,
    ContentItemType Type,
    bool IsRequired,
    string Language,
    string Title,
    string? Body,
    string? VideoReference);

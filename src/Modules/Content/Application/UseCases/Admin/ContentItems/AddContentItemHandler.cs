using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Application.Abstractions;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;

public sealed class AddContentItemHandler(DbContext dbContext, IVideoProvider videoProvider)
{
    public async Task<Guid> HandleAsync(AddContentItemCommand command, CancellationToken cancellationToken)
    {
        var sectionExists = await dbContext.Set<Section>().AnyAsync(s => s.Id == command.SectionId, cancellationToken);
        if (!sectionExists)
        {
            throw new NotFoundAppException("The specified section does not exist.");
        }

        Guid? mediaAssetId = null;

        if (command.Type == ContentItemType.Video)
        {
            VideoRegistration registration;
            try
            {
                registration = await videoProvider.RegisterExistingAsync(command.VideoReference!, cancellationToken);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessRuleAppException("VIDEO_REFERENCE_INVALID", "errors.contentItem.videoReferenceInvalid", ex.Message);
            }

            var existingAsset = await dbContext.Set<MediaAsset>()
                .SingleOrDefaultAsync(m => m.Provider == videoProvider.ProviderName && m.ProviderAssetId == registration.ProviderAssetId, cancellationToken);

            if (existingAsset is null)
            {
                existingAsset = MediaAsset.Create(videoProvider.ProviderName, registration.ProviderAssetId);
                existingAsset.MarkReady(registration.ProviderAssetId, registration.DurationSeconds, registration.ThumbnailUrl);
                dbContext.Set<MediaAsset>().Add(existingAsset);
            }

            mediaAssetId = existingAsset.Id;
        }

        var nextSortOrder = await dbContext.Set<ContentItem>()
            .Where(c => c.SectionId == command.SectionId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var item = ContentItem.Create(command.SectionId, command.Type, nextSortOrder + 1, command.IsRequired, mediaAssetId);
        dbContext.Set<ContentItem>().Add(item);

        dbContext.Set<ContentItemTranslation>().Add(
            ContentItemTranslation.Create(item.Id, command.Language, command.Title, command.Body));

        await dbContext.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}

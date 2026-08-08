using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;

public sealed class DeleteContentItemHandler(DbContext dbContext)
{
    public async Task HandleAsync(Guid contentItemId, CancellationToken cancellationToken)
    {
        var item = await dbContext.Set<ContentItem>().SingleOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new NotFoundAppException("The specified content item does not exist.");

        dbContext.Set<ContentItem>().Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

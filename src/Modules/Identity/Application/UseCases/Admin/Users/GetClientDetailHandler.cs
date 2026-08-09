using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Identity.Application.UseCases.Admin.Users;

public sealed class GetClientDetailHandler(DbContext dbContext)
{
    public async Task<ClientDetailDto> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.IsActive,
                u.EmailVerifiedAtUtc,
                u.CreatedAt,
                Roles = u.UserRoles.Select(ur => new RoleSummaryDto(ur.Role.Id, ur.Role.Name)).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundAppException("The specified client does not exist.");

        return new ClientDetailDto(user.Id, user.Email, user.IsActive, user.EmailVerifiedAtUtc is not null, user.EmailVerifiedAtUtc, user.CreatedAt, user.Roles);
    }
}

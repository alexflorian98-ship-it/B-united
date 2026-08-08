using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.Revoke;

/// <summary>Revokes every active refresh token for the authenticated caller (logout of all
/// devices/sessions). Requires an authenticated caller — see <c>AuthController.LogoutAll</c>.</summary>
public sealed class RevokeAllSessionsHandler(
    DbContext dbContext,
    TimeProvider timeProvider,
    ILogger<RevokeAllSessionsHandler> logger)
{
    public async Task HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var activeTokens = await dbContext.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "identity.logout_all_sessions: UserId {UserId}, RevokedCount {Count}",
            userId,
            activeTokens.Count);
    }
}

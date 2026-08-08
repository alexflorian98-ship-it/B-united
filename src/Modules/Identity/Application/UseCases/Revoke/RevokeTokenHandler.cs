using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUnited.Modules.Identity.Application.UseCases.Revoke;

/// <summary>Logout for a single session. Idempotent — an unknown or already-revoked token
/// is treated as already logged out rather than an error.</summary>
public sealed class RevokeTokenHandler(
    DbContext dbContext,
    ISecureTokenGenerator tokenGenerator,
    TimeProvider timeProvider,
    ILogger<RevokeTokenHandler> logger)
{
    public async Task HandleAsync(RevokeTokenCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = tokenGenerator.Hash(command.RefreshToken);
        var token = await dbContext.Set<RefreshToken>()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.RevokedAtUtc is not null)
        {
            return;
        }

        token.Revoke(timeProvider.GetUtcNow().UtcDateTime);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("identity.logout: UserId {UserId}", token.UserId);
    }
}

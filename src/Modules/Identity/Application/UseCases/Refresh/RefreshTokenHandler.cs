using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Identity.Application.Abstractions;
using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Application.UseCases.Common;
using BUnited.Modules.Identity.Application.UseCases.Login;
using BUnited.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Identity.Application.UseCases.Refresh;

public sealed class RefreshTokenHandler(
    DbContext dbContext,
    IJwtTokenGenerator jwtTokenGenerator,
    ISecureTokenGenerator tokenGenerator,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider,
    ILogger<RefreshTokenHandler> logger)
{
    public async Task<TokenPairResult> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = tokenGenerator.Hash(command.RefreshToken);

        var existingToken = await dbContext.Set<RefreshToken>()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
        {
            throw InvalidToken();
        }

        if (existingToken.RevokedAtUtc is not null)
        {
            // Reuse of an already-rotated/revoked token: treat it as a compromise signal and
            // revoke the whole family so a stolen token can't keep rotating indefinitely.
            await RevokeFamilyAsync(existingToken.FamilyId, utcNow, cancellationToken);

            logger.LogWarning(
                "identity.refresh_token_reuse_detected: UserId {UserId}, FamilyId {FamilyId}",
                existingToken.UserId,
                existingToken.FamilyId);

            throw InvalidToken();
        }

        if (!existingToken.IsActive(utcNow))
        {
            throw InvalidToken();
        }

        var user = await dbContext.Set<User>().SingleAsync(u => u.Id == existingToken.UserId, cancellationToken);

        var permissionKeys = await LoginHandler.LoadPermissionKeysAsync(dbContext, user.Id, cancellationToken);
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, permissionKeys);

        var (rawRefreshToken, newTokenHash) = tokenGenerator.Generate();
        var refreshTokenExpiresAtUtc = utcNow.AddDays(jwtOptions.Value.RefreshTokenLifetimeDays);
        var rotatedToken = existingToken.IssueRotated(newTokenHash, utcNow, refreshTokenExpiresAtUtc);

        existingToken.Revoke(utcNow, rotatedToken.Id);
        dbContext.Set<RefreshToken>().Add(rotatedToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent /auth/refresh call for the same token committed its own rotation
            // first (see RefreshTokenConfiguration's concurrency token on RevokedAtUtc) — this
            // caller loses the race. Fail the same safe, generic way as any other invalid token,
            // never exposing that a race occurred.
            logger.LogWarning(
                "identity.refresh_token_concurrent_rotation_lost: UserId {UserId}", existingToken.UserId);
            throw InvalidToken();
        }

        logger.LogInformation("identity.refresh_token_rotated: UserId {UserId}", user.Id);

        return new TokenPairResult(accessToken.Token, accessToken.ExpiresAtUtc, rawRefreshToken, refreshTokenExpiresAtUtc);
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var family = await dbContext.Set<RefreshToken>()
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in family)
        {
            token.Revoke(utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static BusinessRuleAppException InvalidToken() =>
        new("REFRESH_TOKEN_INVALID", "errors.refreshTokenInvalid", "The refresh token is invalid, expired, or revoked.");
}

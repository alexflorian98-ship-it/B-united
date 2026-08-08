using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Application.UseCases.Refresh;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class RefreshTokenHandlerTests
{
    private static readonly IOptions<JwtOptions> JwtOptions = Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "this-is-a-test-signing-key-that-is-long-enough-1234567890",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 30,
    });

    private static async Task<(TestDbContext Context, Microsoft.Data.Sqlite.SqliteConnection Connection, User User, string RawToken)> SeedUserWithRefreshTokenAsync()
    {
        var (connection, context) = TestDbContextFactory.Create();
        var user = User.Register("ada@example.com", "hash");
        user.MarkEmailVerified(DateTime.UtcNow);
        context.Users.Add(user);

        var tokenGenerator = new SecureTokenGenerator();
        var (rawToken, tokenHash) = tokenGenerator.Generate();
        var refreshToken = RefreshToken.IssueNew(user.Id, tokenHash, DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        context.RefreshTokens.Add(refreshToken);

        await context.SaveChangesAsync();

        return (context, connection, user, rawToken);
    }

    private static RefreshTokenHandler CreateHandler(TestDbContext context) => new(
        context,
        new JwtTokenGenerator(JwtOptions),
        new SecureTokenGenerator(),
        JwtOptions,
        TimeProvider.System,
        NullLogger<RefreshTokenHandler>.Instance);

    [Fact]
    public async Task Rotates_the_token_keeping_the_same_family()
    {
        var (context, connection, user, rawToken) = await SeedUserWithRefreshTokenAsync();
        using var _ = connection;
        using var __ = context;

        var originalTokenRow = await context.RefreshTokens.SingleAsync(t => t.UserId == user.Id);
        var originalFamilyId = originalTokenRow.FamilyId;

        var handler = CreateHandler(context);
        var result = await handler.HandleAsync(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.NotEqual(rawToken, result.RefreshToken);

        var allTokens = await context.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        Assert.Equal(2, allTokens.Count);
        Assert.All(allTokens, t => Assert.Equal(originalFamilyId, t.FamilyId));

        var revoked = allTokens.Single(t => t.Id == originalTokenRow.Id);
        Assert.NotNull(revoked.RevokedAtUtc);
    }

    [Fact]
    public async Task Reusing_an_already_rotated_token_revokes_the_whole_family()
    {
        var (context, connection, _, rawToken) = await SeedUserWithRefreshTokenAsync();
        using var _ = connection;
        using var __ = context;

        var handler = CreateHandler(context);

        // First use: legitimate rotation.
        var firstResult = await handler.HandleAsync(new RefreshTokenCommand(rawToken), CancellationToken.None);

        // Second use of the SAME (now-revoked) original token: simulates a stolen token.
        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new RefreshTokenCommand(rawToken), CancellationToken.None));
        Assert.Equal("REFRESH_TOKEN_INVALID", exception.Code);

        // The legitimately-rotated token must ALSO be revoked now (whole family compromised).
        await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new RefreshTokenCommand(firstResult.RefreshToken), CancellationToken.None));
    }

    [Fact]
    public async Task Unknown_token_is_rejected()
    {
        var (context, connection, _, _) = await SeedUserWithRefreshTokenAsync();
        using var _ = connection;
        using var __ = context;

        var handler = CreateHandler(context);

        var exception = await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => handler.HandleAsync(new RefreshTokenCommand("not-a-real-token"), CancellationToken.None));

        Assert.Equal("REFRESH_TOKEN_INVALID", exception.Code);
    }
}

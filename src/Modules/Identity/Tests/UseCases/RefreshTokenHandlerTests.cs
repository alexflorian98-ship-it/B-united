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

    /// <summary>Deterministic, non-flaky proof of the fix in RefreshTokenConfiguration (concurrency
    /// token on RevokedAtUtc): two DbContexts both load the SAME still-active row (simulating two
    /// concurrent /auth/refresh requests that both read before either writes), then both revoke and
    /// save. Without the concurrency token, both UPDATEs would blindly succeed (last write wins),
    /// silently allowing two branching rotations of one token. With it, the second SaveChangesAsync's
    /// generated UPDATE ... WHERE RevokedAtUtc = @originalNullValue matches zero rows once the first
    /// has committed, so EF Core throws DbUpdateConcurrencyException.</summary>
    [Fact]
    public async Task Two_contexts_racing_to_revoke_the_same_token_row_the_second_write_fails_with_a_concurrency_conflict()
    {
        var (context1, connection, user, rawToken) = await SeedUserWithRefreshTokenAsync();
        using var _ = connection;
        using var __ = context1;

        var tokenHash = new SecureTokenGenerator().Hash(rawToken);

        // A second DbContext on the SAME connection/transaction-less session, loading its own
        // independent tracked copy of the row before context1 writes anything — mirrors what two
        // separate request-scoped DbContexts would each see if their reads overlapped.
        await using var context2 = new TestDbContext(
            new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        var rowInContext1 = await context1.RefreshTokens.SingleAsync(t => t.TokenHash == tokenHash);
        var rowInContext2 = await context2.RefreshTokens.SingleAsync(t => t.TokenHash == tokenHash);

        rowInContext1.Revoke(DateTime.UtcNow);
        await context1.SaveChangesAsync();

        rowInContext2.Revoke(DateTime.UtcNow);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context2.SaveChangesAsync());
    }

    /// <summary>Two /auth/refresh calls racing on the SAME still-active token (e.g. a stolen token
    /// used at nearly the same instant as the legitimate client). Uses two independent SQLite
    /// connections against one shared-cache in-memory database — not a single shared connection
    /// object — so the reads/writes are genuinely concurrent I/O, the same way two separate
    /// ASP.NET Core request-scoped DbContexts racing against real PostgreSQL would be. Whichever
    /// safety net actually catches the loser (the concurrency-token conflict above, or the cheap
    /// "already revoked" reuse check if the race isn't tight enough to hit the DB-level conflict —
    /// both are legitimate outcomes and both are exercised across repeated runs of this test) MUST
    /// produce the same invariant: never more than one successful rotation from a single token, and
    /// the loser always fails with the standard safe error, never a 500.</summary>
    [Fact]
    public async Task Concurrent_refresh_of_the_same_token_never_lets_more_than_one_caller_succeed()
    {
        var dataSource = $"DataSource=file:refresh-race-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        // A shared-cache SQLite in-memory database is destroyed the instant its last connection
        // closes. Every DbContext below opens/closes its own connection per operation, so this
        // one is kept open for the whole test purely to keep the database alive.
        await using var keepAlive = new Microsoft.Data.Sqlite.SqliteConnection(dataSource);
        await keepAlive.OpenAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(dataSource).Options;

        await using (var schemaContext = new TestDbContext(options))
        {
            await schemaContext.Database.EnsureCreatedAsync();
        }

        string rawToken;
        await using (var seedContext = new TestDbContext(options))
        {
            var user = User.Register("race@example.com", "hash");
            user.MarkEmailVerified(DateTime.UtcNow);
            seedContext.Users.Add(user);

            var (raw, tokenHash) = new SecureTokenGenerator().Generate();
            rawToken = raw;
            seedContext.RefreshTokens.Add(RefreshToken.IssueNew(user.Id, tokenHash, DateTime.UtcNow, DateTime.UtcNow.AddDays(30)));

            await seedContext.SaveChangesAsync();
        }

        await using var context1 = new TestDbContext(options);
        await using var context2 = new TestDbContext(options);

        var command = new RefreshTokenCommand(rawToken);
        var race = await Task.WhenAll(
            RunAsync(CreateHandler(context1), command),
            RunAsync(CreateHandler(context2), command));

        Assert.True(race.Count(r => r.Succeeded) <= 1, "More than one concurrent caller rotated the same token.");
        Assert.All(race.Where(r => !r.Succeeded), r => Assert.Equal("REFRESH_TOKEN_INVALID", r.ErrorCode));
    }

    private static async Task<(bool Succeeded, string? ErrorCode, string? NewRefreshToken)> RunAsync(
        RefreshTokenHandler handler, RefreshTokenCommand command)
    {
        try
        {
            var result = await handler.HandleAsync(command, CancellationToken.None);
            return (true, null, result.RefreshToken);
        }
        catch (BusinessRuleAppException ex)
        {
            return (false, ex.Code, null);
        }
    }
}

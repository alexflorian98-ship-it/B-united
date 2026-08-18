using BUnited.Modules.Identity.Application.UseCases.Refresh;
using BUnited.Modules.Identity.Application.UseCases.Revoke;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Identity.Application.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Identity.Tests.UseCases;

/// <summary>Security-gap-closure item #2 (auth/token lifecycle): "revoke-all/logout-everywhere"
/// had a handler (<see cref="RevokeAllSessionsHandler"/>, wired at <c>AuthController.LogoutAll</c>)
/// but no test anywhere in the module. Proves it revokes every active session for the caller,
/// leaves another user's sessions untouched, and that every revoked token really stops working
/// afterward — not just that a database flag flips.</summary>
public sealed class RevokeAllSessionsHandlerTests
{
    private static readonly IOptions<JwtOptions> JwtOptions = Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "this-is-a-test-signing-key-that-is-long-enough-1234567890",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 30,
    });

    [Fact]
    public async Task Revokes_every_active_session_for_the_caller_and_leaves_another_users_sessions_untouched()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var caller = User.Register("caller@example.com", "hash");
        var otherUser = User.Register("other@example.com", "hash");
        context.Users.AddRange(caller, otherUser);

        var tokenGenerator = new SecureTokenGenerator();
        var (callerRawA, callerHashA) = tokenGenerator.Generate();
        var (callerRawB, callerHashB) = tokenGenerator.Generate();
        var (otherRaw, otherHash) = tokenGenerator.Generate();

        context.RefreshTokens.AddRange(
            RefreshToken.IssueNew(caller.Id, callerHashA, DateTime.UtcNow, DateTime.UtcNow.AddDays(30)),
            RefreshToken.IssueNew(caller.Id, callerHashB, DateTime.UtcNow, DateTime.UtcNow.AddDays(30)),
            RefreshToken.IssueNew(otherUser.Id, otherHash, DateTime.UtcNow, DateTime.UtcNow.AddDays(30)));
        await context.SaveChangesAsync();

        var revokeAllHandler = new RevokeAllSessionsHandler(context, TimeProvider.System, NullLogger<RevokeAllSessionsHandler>.Instance);
        await revokeAllHandler.HandleAsync(caller.Id, CancellationToken.None);

        var callerTokens = await context.RefreshTokens.Where(t => t.UserId == caller.Id).ToListAsync();
        Assert.All(callerTokens, t => Assert.NotNull(t.RevokedAtUtc));

        var otherUserToken = await context.RefreshTokens.SingleAsync(t => t.UserId == otherUser.Id);
        Assert.Null(otherUserToken.RevokedAtUtc);

        // Every revoked session must actually stop working, not just carry a flag.
        var refreshHandler = new RefreshTokenHandler(
            context, new JwtTokenGenerator(JwtOptions), tokenGenerator, JwtOptions, TimeProvider.System, NullLogger<RefreshTokenHandler>.Instance);

        await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => refreshHandler.HandleAsync(new RefreshTokenCommand(callerRawA), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleAppException>(
            () => refreshHandler.HandleAsync(new RefreshTokenCommand(callerRawB), CancellationToken.None));

        // The other user's session survives revoke-all and can still rotate normally.
        var otherResult = await refreshHandler.HandleAsync(new RefreshTokenCommand(otherRaw), CancellationToken.None);
        Assert.NotEqual(otherRaw, otherResult.RefreshToken);
    }

    [Fact]
    public async Task Is_a_safe_no_op_when_the_caller_has_no_active_sessions()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var handler = new RevokeAllSessionsHandler(context, TimeProvider.System, NullLogger<RevokeAllSessionsHandler>.Instance);

        // Must not throw for a user with zero refresh tokens.
        await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);
    }
}

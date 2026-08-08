using BUnited.Modules.Identity.Application.UseCases.Revoke;
using BUnited.Modules.Identity.Domain.Entities;
using BUnited.Modules.Identity.Infrastructure.Security;
using BUnited.Modules.Identity.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BUnited.Modules.Identity.Tests.UseCases;

public sealed class RevokeTokenHandlerTests
{
    [Fact]
    public async Task Revokes_an_active_token()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var user = User.Register("ada@example.com", "hash");
        context.Users.Add(user);

        var tokenGenerator = new SecureTokenGenerator();
        var (rawToken, tokenHash) = tokenGenerator.Generate();
        context.RefreshTokens.Add(RefreshToken.IssueNew(user.Id, tokenHash, DateTime.UtcNow, DateTime.UtcNow.AddDays(30)));
        await context.SaveChangesAsync();

        var handler = new RevokeTokenHandler(context, tokenGenerator, TimeProvider.System, NullLogger<RevokeTokenHandler>.Instance);
        await handler.HandleAsync(new RevokeTokenCommand(rawToken), CancellationToken.None);

        var token = await context.RefreshTokens.SingleAsync(t => t.UserId == user.Id);
        Assert.NotNull(token.RevokedAtUtc);
    }

    [Fact]
    public async Task Is_idempotent_for_an_unknown_token()
    {
        var (connection, context) = TestDbContextFactory.Create();
        using var _ = connection;
        using var __ = context;

        var handler = new RevokeTokenHandler(
            context,
            new SecureTokenGenerator(),
            TimeProvider.System,
            NullLogger<RevokeTokenHandler>.Instance);

        // Must not throw.
        await handler.HandleAsync(new RevokeTokenCommand("unknown-token"), CancellationToken.None);
    }
}

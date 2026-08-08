namespace BUnited.Modules.Identity.Application.Abstractions;

public interface IJwtTokenGenerator
{
    JwtAccessToken GenerateAccessToken(Guid userId, string email, IReadOnlyCollection<string> permissionKeys);
}

public sealed record JwtAccessToken(string Token, DateTime ExpiresAtUtc);

namespace BUnited.Modules.Identity.Application.UseCases.Common;

public sealed record TokenPairResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

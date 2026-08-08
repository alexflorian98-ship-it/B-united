using System.Security.Cryptography;
using System.Text;
using BUnited.Modules.Identity.Application.Abstractions;

namespace BUnited.Modules.Identity.Infrastructure.Security;

public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    private const int TokenByteLength = 32;

    public (string RawToken, string TokenHash) Generate()
    {
        var rawToken = GenerateRawToken();
        return (rawToken, Hash(rawToken));
    }

    public string Hash(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(hashBytes);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

using BUnited.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace BUnited.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Wraps ASP.NET Core Identity's PBKDF2-based <see cref="PasswordHasher{TUser}"/> without
/// pulling in the rest of the Identity system. The generic parameter is unused by the default
/// implementation, so a dummy type is fine here.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash)
    {
        var result = _inner.VerifyHashedPassword(null!, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

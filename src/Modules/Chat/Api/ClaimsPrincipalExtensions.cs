using System.Security.Claims;

namespace BUnited.Modules.Chat.Api;

/// <summary>Small duplicate of Identity.Api's <c>GetUserId()</c> — see the identical comment on
/// every other module's copy of this class for why.</summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("The current principal has no 'sub' claim.");
        return Guid.Parse(value);
    }
}

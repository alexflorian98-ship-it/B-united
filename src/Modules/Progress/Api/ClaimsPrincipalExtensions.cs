using System.Security.Claims;

namespace BUnited.Modules.Progress.Api;

/// <summary>Same deliberate small duplicate as Content.Api's — see that copy's remarks.</summary>
internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("The current principal has no 'sub' claim.");

        return Guid.Parse(subject);
    }
}

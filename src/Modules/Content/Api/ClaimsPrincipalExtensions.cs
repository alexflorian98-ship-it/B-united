using System.Security.Claims;

namespace BUnited.Modules.Content.Api;

/// <summary>Small, deliberate duplicate of Identity.Api's own extension of the same name —
/// referencing Identity.Api from here would be an odd Api-to-Api coupling for five lines of
/// JWT-claim reading, and both modules only need the same standard "sub" claim. Uses the literal
/// "sub" rather than <c>JwtRegisteredClaimNames.Sub</c> (avoids a package reference just for one
/// constant) — safe because <c>JwtAuthenticationExtensions</c> sets <c>MapInboundClaims = false</c>
/// process-wide, so the claim type on the principal really is the unmapped "sub".</summary>
internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("The current principal has no 'sub' claim.");

        return Guid.Parse(subject);
    }
}

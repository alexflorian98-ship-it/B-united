using System.Security.Claims;

namespace BUnited.Modules.Questionnaires.Api;

/// <summary>Small duplicate of Identity.Api's <c>GetUserId()</c> — avoids a package reference
/// just for the <c>JwtRegisteredClaimNames.Sub</c> constant. Safe because
/// <c>JwtAuthenticationExtensions</c> sets <c>MapInboundClaims = false</c> process-wide, so the
/// inbound claim type is always the literal "sub".</summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("The current principal has no 'sub' claim.");
        return Guid.Parse(value);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BUnited.Modules.Identity.Api;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The current principal has no 'sub' claim.");

        return Guid.Parse(subject);
    }
}

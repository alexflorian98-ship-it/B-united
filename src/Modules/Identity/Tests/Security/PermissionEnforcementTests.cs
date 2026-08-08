using System.Net;
using System.Net.Http.Headers;
using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Identity.Tests.Security;

/// <summary>
/// End-to-end proof (real HTTP through the real authentication/authorization middleware, see
/// <see cref="PermissionTestHostFixture"/>) that every seeded permission actually gates access —
/// not just that the policy object is registered (that's <c>IdentityAuthorizationExtensionsTests</c>).
/// </summary>
public sealed class PermissionEnforcementTests(PermissionTestHostFixture fixture) : IClassFixture<PermissionTestHostFixture>
{
    public static IEnumerable<object[]> AllPermissionKeys() =>
        WellKnownPermissions.All.Select(p => new object[] { p.Key });

    [Theory]
    [MemberData(nameof(AllPermissionKeys))]
    public async Task Authorized_caller_with_the_matching_permission_claim_succeeds(string permissionKey)
    {
        var token = fixture.IssueToken([permissionKey]);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{permissionKey}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AllPermissionKeys))]
    public async Task Authenticated_caller_without_the_matching_permission_claim_is_forbidden(string permissionKey)
    {
        // Authenticated, but holding a DIFFERENT permission than the one the endpoint requires.
        var otherKey = WellKnownPermissions.All.Select(p => p.Key).First(key => key != permissionKey);
        var token = fixture.IssueToken([otherKey]);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{permissionKey}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AllPermissionKeys))]
    public async Task Unauthenticated_caller_is_rejected_with_401(string permissionKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{permissionKey}");

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_caller_is_rejected_on_a_plain_authenticated_only_endpoint()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/authenticated-only");

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_caller_without_any_permission_claims_succeeds_on_the_authenticated_only_endpoint()
    {
        var token = fixture.IssueToken([]);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/authenticated-only");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task An_expired_token_is_rejected_with_401_even_with_the_right_permission_claim()
    {
        var expiredOptions = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "this-is-a-test-signing-key-that-is-long-enough-1234567890",
            AccessTokenLifetimeMinutes = -5,
            RefreshTokenLifetimeDays = 30,
        });
        var expiredToken = new JwtTokenGenerator(expiredOptions)
            .GenerateAccessToken(Guid.NewGuid(), "expired@example.com", [WellKnownPermissions.ContentView])
            .Token;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{WellKnownPermissions.ContentView}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

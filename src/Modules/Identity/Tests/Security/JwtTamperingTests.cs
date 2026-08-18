using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BUnited.Modules.Identity.Application.Configuration;
using BUnited.Modules.Identity.Domain;
using BUnited.Modules.Identity.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace BUnited.Modules.Identity.Tests.Security;

/// <summary>Security-gap-closure item #2 (auth/token lifecycle): malformed tokens and claim
/// tampering (permissions/issuer/audience/signature) had no dedicated coverage —
/// <see cref="PermissionEnforcementTests"/> only covered missing-claim (403) and expiry (401).
/// Drives the real JWT bearer authentication middleware over real HTTP (see
/// <see cref="PermissionTestHostFixture"/>), never asserting on the JWT library directly, since
/// what actually matters is what <c>AddIdentityJwtAuthentication</c>'s configured
/// <c>TokenValidationParameters</c> does with each input.</summary>
public sealed class JwtTamperingTests(PermissionTestHostFixture fixture) : IClassFixture<PermissionTestHostFixture>
{
    private static readonly IOptions<JwtOptions> ValidOptions = Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "this-is-a-test-signing-key-that-is-long-enough-1234567890",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 30,
    });

    [Theory]
    [InlineData("not-a-jwt-at-all")]
    [InlineData("")]
    [InlineData("a.b")]
    [InlineData("a.b.c.d")]
    [InlineData("   ")]
    public async Task Malformed_token_strings_are_rejected_with_401(string malformedToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{WellKnownPermissions.ContentView}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", malformedToken);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_syntactically_valid_token_signed_with_a_different_key_is_rejected()
    {
        var wrongKeyOptions = Options.Create(new JwtOptions
        {
            Issuer = ValidOptions.Value.Issuer,
            Audience = ValidOptions.Value.Audience,
            SigningKey = "a-completely-different-signing-key-thats-also-long-enough-000",
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 30,
        });
        var forgedToken = new JwtTokenGenerator(wrongKeyOptions)
            .GenerateAccessToken(Guid.NewGuid(), "attacker@example.com", [WellKnownPermissions.ContentView])
            .Token;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{WellKnownPermissions.ContentView}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", forgedToken);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_with_a_tampered_payload_segment_is_rejected_even_with_the_original_signature()
    {
        // Mints a real, validly-signed token holding no permissions, then swaps only the payload
        // (base64url) segment for one claiming an extra permission — keeping the original
        // signature. Proves the signature actually covers the payload (the classic "just edit the
        // JSON" JWT attack), not merely that a well-formed unsigned token is rejected.
        var genuineToken = new JwtTokenGenerator(ValidOptions)
            .GenerateAccessToken(Guid.NewGuid(), "attacker@example.com", [])
            .Token;
        var segments = genuineToken.Split('.');
        Assert.Equal(3, segments.Length);

        var tamperedPayloadJson = Encoding.UTF8.GetString(Base64UrlDecode(segments[1]))
            .Replace("\"email\":\"attacker@example.com\"", $"\"email\":\"attacker@example.com\",\"permission\":\"{WellKnownPermissions.ContentView}\"");
        var tamperedToken = $"{segments[0]}.{Base64UrlEncode(Encoding.UTF8.GetBytes(tamperedPayloadJson))}.{segments[2]}";

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{WellKnownPermissions.ContentView}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_with_the_wrong_issuer_is_rejected()
    {
        var wrongIssuerOptions = Options.Create(new JwtOptions
        {
            Issuer = "a-different-issuer",
            Audience = ValidOptions.Value.Audience,
            SigningKey = ValidOptions.Value.SigningKey,
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 30,
        });
        var token = new JwtTokenGenerator(wrongIssuerOptions)
            .GenerateAccessToken(Guid.NewGuid(), "attacker@example.com", [WellKnownPermissions.ContentView])
            .Token;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{WellKnownPermissions.ContentView}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_with_the_wrong_audience_is_rejected()
    {
        var wrongAudienceOptions = Options.Create(new JwtOptions
        {
            Issuer = ValidOptions.Value.Issuer,
            Audience = "a-different-audience",
            SigningKey = ValidOptions.Value.SigningKey,
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 30,
        });
        var token = new JwtTokenGenerator(wrongAudienceOptions)
            .GenerateAccessToken(Guid.NewGuid(), "attacker@example.com", [WellKnownPermissions.ContentView])
            .Token;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{WellKnownPermissions.ContentView}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_none_algorithm_token_is_rejected()
    {
        // The classic "alg: none" JWT bypass: a header claiming no signature algorithm, a
        // legitimate-looking payload, and an empty signature segment.
        var header = Base64UrlEncode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(
            $$"""{"sub":"{{Guid.NewGuid()}}","email":"attacker@example.com","permission":"{{WellKnownPermissions.ContentView}}","iss":"{{ValidOptions.Value.Issuer}}","aud":"{{ValidOptions.Value.Audience}}","exp":{{DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()}}}"""));
        var noneAlgToken = $"{header}.{payload}.";

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/test/{WellKnownPermissions.ContentView}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", noneAlgToken);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

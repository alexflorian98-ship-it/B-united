using BUnited.Modules.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BUnited.Modules.Identity.Tests.Security;

/// <summary>Security-gap-closure item #8 ("placeholder secrets rejected at startup"): the JWT
/// signing key length check alone does not catch .env.example's own documented example value —
/// it is long enough (58 chars) to pass. Since .env.example is committed to the repo, deploying
/// to Production with that literal string still in place would let anyone forge a validly-signed
/// JWT for any user. Only enforced in Production — Development/demo environments legitimately run
/// with the documented example value.</summary>
public sealed class ProductionSecretSafetyTests
{
    private sealed class FakeEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = ".";

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static IConfiguration BuildConfiguration(string signingKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "bunited",
                ["Jwt:Audience"] = "bunited-clients",
                ["Jwt:SigningKey"] = signingKey,
            })
            .Build();

    [Fact]
    public void Refuses_to_start_in_Production_with_the_documented_placeholder_signing_key()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("change-me-to-a-random-base64-value-at-least-32-bytes-long");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddIdentityJwtAuthentication(configuration, new FakeEnvironment(Environments.Production)));

        Assert.Contains("Production", exception.Message);
        Assert.Contains("SigningKey", exception.Message);
    }

    [Fact]
    public void Allows_the_documented_placeholder_signing_key_outside_Production()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("change-me-to-a-random-base64-value-at-least-32-bytes-long");

        var exception = Record.Exception(() =>
            services.AddIdentityJwtAuthentication(configuration, new FakeEnvironment(Environments.Development)));

        Assert.Null(exception);
    }

    [Fact]
    public void Allows_a_real_random_signing_key_in_Production()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("a-real-randomly-generated-signing-key-that-is-long-enough-987654321");

        var exception = Record.Exception(() =>
            services.AddIdentityJwtAuthentication(configuration, new FakeEnvironment(Environments.Production)));

        Assert.Null(exception);
    }

    [Fact]
    public void Still_rejects_a_too_short_key_regardless_of_environment()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("too-short");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddIdentityJwtAuthentication(configuration, new FakeEnvironment(Environments.Production)));

        Assert.Contains("too short", exception.Message);
    }
}

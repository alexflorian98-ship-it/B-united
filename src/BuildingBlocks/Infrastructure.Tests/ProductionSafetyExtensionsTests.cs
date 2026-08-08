using BUnited.BuildingBlocks.Application.Access;
using BUnited.BuildingBlocks.Infrastructure.Safety;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BUnited.BuildingBlocks.Infrastructure.Tests;

public sealed class ProductionSafetyExtensionsTests
{
    private sealed class FakeAdapter : IDemoOnlyAdapter
    {
    }

    private sealed class RealAdapter
    {
    }

    private sealed class FakeEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = ".";

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public void Throws_in_Production_when_a_demo_only_adapter_is_registered()
    {
        var services = new ServiceCollection();
        services.AddScoped<FakeAdapter>();

        Assert.Throws<InvalidOperationException>(() =>
            services.VerifyNoDemoOnlyAdaptersInProduction(new FakeEnvironment(Environments.Production)));
    }

    [Fact]
    public void Does_not_throw_in_Production_when_no_demo_only_adapter_is_registered()
    {
        var services = new ServiceCollection();
        services.AddScoped<RealAdapter>();

        var exception = Record.Exception(() =>
            services.VerifyNoDemoOnlyAdaptersInProduction(new FakeEnvironment(Environments.Production)));

        Assert.Null(exception);
    }

    [Fact]
    public void Does_not_throw_outside_Production_even_with_a_demo_only_adapter_registered()
    {
        var services = new ServiceCollection();
        services.AddScoped<FakeAdapter>();

        var exception = Record.Exception(() =>
            services.VerifyNoDemoOnlyAdaptersInProduction(new FakeEnvironment(Environments.Development)));

        Assert.Null(exception);
    }
}

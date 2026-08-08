using BUnited.BuildingBlocks.Observability.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BUnited.BuildingBlocks.Observability.Tests;

public sealed class SensitiveDataDestructuringPolicyTests
{
    private sealed class CapturingSink : ILogEventSink
    {
        public LogEvent? LastEvent { get; private set; }

        public void Emit(LogEvent logEvent) => LastEvent = logEvent;
    }

    private sealed class SamplePayload
    {
        public string Username { get; set; } = string.Empty;

        [SensitiveLogValue]
        public string Password { get; set; } = string.Empty;
    }

    private sealed class PlainPayload
    {
        public string Value { get; set; } = string.Empty;
    }

    [Fact]
    public void Redacts_properties_marked_with_SensitiveLogValueAttribute()
    {
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var payload = new SamplePayload { Username = "ada", Password = "super-secret" };
        logger.Information("{@Payload}", payload);

        var structure = Assert.IsType<StructureValue>(sink.LastEvent!.Properties["Payload"]);
        var username = Assert.IsType<ScalarValue>(
            structure.Properties.Single(p => p.Name == nameof(SamplePayload.Username)).Value);
        var password = Assert.IsType<ScalarValue>(
            structure.Properties.Single(p => p.Name == nameof(SamplePayload.Password)).Value);

        Assert.Equal("ada", username.Value);
        Assert.Equal("***REDACTED***", password.Value);
    }

    [Fact]
    public void Leaves_objects_without_sensitive_properties_to_the_default_policy()
    {
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("{@Payload}", new PlainPayload { Value = "hello" });

        var structure = Assert.IsType<StructureValue>(sink.LastEvent!.Properties["Payload"]);
        Assert.Equal(nameof(PlainPayload), structure.TypeTag);
        var value = Assert.IsType<ScalarValue>(
            structure.Properties.Single(p => p.Name == nameof(PlainPayload.Value)).Value);
        Assert.Equal("hello", value.Value);
    }
}

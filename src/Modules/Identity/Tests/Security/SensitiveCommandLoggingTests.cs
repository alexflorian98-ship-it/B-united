using BUnited.BuildingBlocks.Observability.Logging;
using BUnited.Modules.Identity.Application.UseCases.Login;
using BUnited.Modules.Identity.Application.UseCases.PasswordReset;
using BUnited.Modules.Identity.Application.UseCases.Refresh;
using BUnited.Modules.Identity.Application.UseCases.Register;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BUnited.Modules.Identity.Tests.Security;

/// <summary>Security-gap-closure item #5 (log-leakage): <see cref="SensitiveLogValueAttribute"/>
/// existed but, before this pass, was never actually applied to any real command/DTO anywhere in
/// the codebase — a repo-wide search confirmed zero real usages, only the destructuring policy's
/// own unit test using a synthetic sample class. That meant zero defense-in-depth: if any of
/// these real command types were ever accidentally logged in full (e.g. `logger.LogInformation("{@Command}",
/// command)` added later during debugging), nothing would have redacted the password/token.
/// This proves the fix — the real production command types now carry the attribute and the real
/// Serilog policy actually redacts them — not just a synthetic stand-in class.</summary>
public sealed class SensitiveCommandLoggingTests
{
    private sealed class CapturingSink : ILogEventSink
    {
        public LogEvent? LastEvent { get; private set; }

        public void Emit(LogEvent logEvent) => LastEvent = logEvent;
    }

    private static (Logger Logger, CapturingSink Sink) CreateLogger()
    {
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Sink(sink)
            .CreateLogger();
        return (logger, sink);
    }

    private static string RedactedValueOf(LogEvent logEvent, string property)
    {
        var structure = Assert.IsType<StructureValue>(logEvent.Properties["Command"]);
        var scalar = Assert.IsType<ScalarValue>(structure.Properties.Single(p => p.Name == property).Value);
        return (string)scalar.Value!;
    }

    [Fact]
    public void LoginCommand_password_is_redacted_if_ever_logged()
    {
        var (logger, sink) = CreateLogger();
        logger.Information("{@Command}", new LoginCommand("ada@example.com", "super-secret-password"));

        Assert.Equal("ada@example.com", RedactedValueOf(sink.LastEvent!, nameof(LoginCommand.Email)));
        Assert.Equal("***REDACTED***", RedactedValueOf(sink.LastEvent!, nameof(LoginCommand.Password)));
    }

    [Fact]
    public void RegisterUserCommand_password_is_redacted_if_ever_logged()
    {
        var (logger, sink) = CreateLogger();
        logger.Information("{@Command}", new RegisterUserCommand("ada@example.com", "super-secret-password"));

        Assert.Equal("***REDACTED***", RedactedValueOf(sink.LastEvent!, nameof(RegisterUserCommand.Password)));
    }

    [Fact]
    public void RefreshTokenCommand_token_is_redacted_if_ever_logged()
    {
        var (logger, sink) = CreateLogger();
        logger.Information("{@Command}", new RefreshTokenCommand("raw-refresh-token-value"));

        Assert.Equal("***REDACTED***", RedactedValueOf(sink.LastEvent!, nameof(RefreshTokenCommand.RefreshToken)));
    }

    [Fact]
    public void ConfirmPasswordResetCommand_token_and_new_password_are_both_redacted_if_ever_logged()
    {
        var (logger, sink) = CreateLogger();
        logger.Information("{@Command}", new ConfirmPasswordResetCommand("reset-token-value", "new-super-secret-password"));

        Assert.Equal("***REDACTED***", RedactedValueOf(sink.LastEvent!, nameof(ConfirmPasswordResetCommand.Token)));
        Assert.Equal("***REDACTED***", RedactedValueOf(sink.LastEvent!, nameof(ConfirmPasswordResetCommand.NewPassword)));
    }
}

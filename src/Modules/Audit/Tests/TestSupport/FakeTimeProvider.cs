namespace BUnited.Modules.Audit.Tests.TestSupport;

internal sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => start;
}

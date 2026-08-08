using BUnited.BuildingBlocks.Observability.CorrelationId;

namespace BUnited.Modules.Audit.Tests.TestSupport;

internal sealed class FakeCorrelationIdAccessor(string correlationId) : ICorrelationIdAccessor
{
    public string CorrelationId { get; } = correlationId;
}

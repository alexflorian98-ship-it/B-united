namespace BUnited.BuildingBlocks.Observability.CorrelationId;

/// <summary>
/// Scoped holder for the current request's correlation id, populated once by
/// <see cref="CorrelationIdMiddleware"/> at the start of the request pipeline.
/// </summary>
public sealed class CorrelationIdContext : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = string.Empty;
}

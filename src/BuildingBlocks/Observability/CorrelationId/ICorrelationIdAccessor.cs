namespace BUnited.BuildingBlocks.Observability.CorrelationId;

/// <summary>
/// Exposes the current request's correlation id to application code (e.g. the standardized
/// error contract's <c>correlationId</c> field) without depending on <c>HttpContext</c> directly.
/// </summary>
public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }
}

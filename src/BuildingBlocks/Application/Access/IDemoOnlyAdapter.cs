namespace BUnited.BuildingBlocks.Application.Access;

/// <summary>Marker for any adapter that simulates a real external integration instead of
/// actually performing it (docs/PROMPT.md §72, P3.32; ADR-010) — e.g. <c>FakePaymentProvider</c>,
/// <c>LoggingNotificationSender</c>. Implementing this is what makes an adapter eligible for the
/// production-safety startup check: the application MUST refuse to start in <c>Production</c>
/// if any service implementing this interface is registered.</summary>
public interface IDemoOnlyAdapter
{
}

namespace BUnited.Modules.Billing.Application.UseCases;

public enum DemoAction
{
    Succeed,
    Fail,
    Refund,
    ChargeBack,
}

public sealed record TriggerDemoEventCommand(Guid UserId, Guid ProgramId, DemoAction Action);

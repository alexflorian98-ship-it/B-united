using BUnited.Modules.Billing.Application.Abstractions;

namespace BUnited.Modules.Billing.Application.UseCases;

public enum DemoAction
{
    Renew,
    FailPayment,
    Cancel,
    Expire,
}

public sealed record TriggerDemoEventCommand(Guid UserId, DemoAction Action);

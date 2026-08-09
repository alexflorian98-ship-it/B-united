using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Billing.Application.Abstractions;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

/// <summary>Local demo-only purchase controls (succeed/fail/refund/chargeback), driving the
/// exact same fake-event pipeline a real provider's test-mode webhook would — see ADR-010.
/// Operates on the caller's most recent purchase for the given program, never anyone else's.</summary>
public sealed class TriggerDemoEventHandler(DbContext dbContext, IPaymentProvider paymentProvider, ProcessProviderEventHandler processProviderEventHandler)
{
    public async Task HandleAsync(TriggerDemoEventCommand command, CancellationToken cancellationToken)
    {
        var purchase = await dbContext.Set<Purchase>()
            .Where(p => p.UserId == command.UserId && p.ProgramId == command.ProgramId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundAppException("No purchase exists for this user and program.");

        var eventType = command.Action switch
        {
            DemoAction.Succeed => ProviderEventType.PaymentSucceeded,
            DemoAction.Fail => ProviderEventType.PaymentFailed,
            DemoAction.Refund => ProviderEventType.PaymentRefunded,
            DemoAction.ChargeBack => ProviderEventType.PaymentChargedBack,
            _ => throw new ArgumentOutOfRangeException(nameof(command)),
        };

        var utcNow = DateTime.UtcNow;
        var providerEvent = paymentProvider.CreateDemoEvent(purchase.Id, eventType, purchase.Amount, purchase.Currency, utcNow);
        await processProviderEventHandler.HandleAsync(providerEvent, cancellationToken);
    }
}

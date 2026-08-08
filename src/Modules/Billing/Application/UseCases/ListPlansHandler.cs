using BUnited.Modules.Billing.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Billing.Application.UseCases;

public sealed class ListPlansHandler(DbContext dbContext)
{
    public async Task<IReadOnlyList<PlanDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var plans = await dbContext.Set<Plan>().AsNoTracking().Where(p => p.IsActive).ToListAsync(cancellationToken);
        var planIds = plans.Select(p => p.Id).ToList();

        var prices = await dbContext.Set<PlanPrice>().AsNoTracking()
            .Where(p => planIds.Contains(p.PlanId) && p.IsActive)
            .ToListAsync(cancellationToken);

        return plans.Select(plan => new PlanDto(
            plan.Id,
            plan.Name,
            plan.Description,
            prices.Where(p => p.PlanId == plan.Id)
                .Select(p => new PlanPriceDto(p.Id, p.Amount, p.Currency, p.Interval.ToString()))
                .ToList()))
            .ToList();
    }
}

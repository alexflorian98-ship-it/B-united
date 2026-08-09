using BUnited.Modules.Admin.Application.Dtos;
using BUnited.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Admin.Application.UseCases;

/// <summary>docs/IMPLEMENTATION_PLAN.md Slice A3 — the purchases/entitlements section of the
/// client detail screen. An ADR-007 read-only cross-module projection over Billing's tables
/// (never Billing's Domain/Infrastructure layers), the same pattern <see cref="GetDashboardHandler"/>
/// already establishes. Deliberately excludes questionnaire/guidance data — client
/// administration must never surface that (CLAUDE.md: "Administrators have no implicit access to
/// questionnaire answers or guidance").</summary>
public sealed class GetClientCommerceSummaryHandler(DbContext dbContext)
{
    public async Task<ClientCommerceSummaryDto> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var purchases = await dbContext.Set<Purchase>().AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var entitlements = await dbContext.Set<ProgramEntitlement>().AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.GrantedAtUtc)
            .ToListAsync(cancellationToken);

        var programIds = purchases.Select(p => p.ProgramId).Concat(entitlements.Select(e => e.ProgramId)).Distinct().ToList();
        var programSlugs = await dbContext.Set<Program>().AsNoTracking()
            .Where(p => programIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Slug })
            .ToDictionaryAsync(p => p.Id, p => p.Slug, cancellationToken);

        return new ClientCommerceSummaryDto(
            purchases.Select(p => new ClientPurchaseSummaryDto(
                p.Id, p.ProgramId, programSlugs.GetValueOrDefault(p.ProgramId), p.ProgramTitleSnapshot, p.Amount, p.Currency, p.Status.ToString(), p.CreatedAt, p.CompletedAtUtc))
                .ToList(),
            entitlements.Select(e => new ClientEntitlementSummaryDto(
                e.ProgramId, programSlugs.GetValueOrDefault(e.ProgramId), e.Status.ToString(), e.GrantedAtUtc, e.RevokedAtUtc))
                .ToList());
    }
}

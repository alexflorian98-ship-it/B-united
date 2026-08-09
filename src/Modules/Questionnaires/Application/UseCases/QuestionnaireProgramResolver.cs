using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases;

/// <summary>Resolves the owning <c>ProgramId</c> for a <see cref="Questionnaire"/> so client
/// handlers can defer to <see cref="BUnited.BuildingBlocks.Application.Access.IProgramAccessContext"/>
/// (ADR-003 per-program entitlement) before touching submission/guidance rows. Unlike Progress's
/// cross-module <c>IContentItemProgramLookup</c> (needed because <c>ContentItemId</c> is owned by
/// Content), no cross-module contract is required here: <c>ProgramId</c> lives directly on
/// Questionnaires' own <see cref="Questionnaire"/> entity, so this is a plain query against this
/// module's own <see cref="DbContext"/>.</summary>
internal static class QuestionnaireProgramResolver
{
    public static async Task<Guid> GetOwningProgramIdAsync(DbContext dbContext, Guid questionnaireId, CancellationToken cancellationToken)
    {
        var programId = await dbContext.Set<Questionnaire>()
            .Where(q => q.Id == questionnaireId)
            .Select(q => (Guid?)q.ProgramId)
            .SingleOrDefaultAsync(cancellationToken);

        return programId ?? throw new NotFoundAppException("The specified questionnaire does not exist.");
    }
}

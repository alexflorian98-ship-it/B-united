using BUnited.BuildingBlocks.Application.DataRights;
using BUnited.Modules.Questionnaires.Application.UseCases.Client;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.DataRights;

/// <summary>Wraps the existing <see cref="ExportMyQuestionnaireDataHandler"/> as the
/// Questionnaires section of the full-account export archive — no logic duplicated.</summary>
public sealed class QuestionnairesUserDataExporter(ExportMyQuestionnaireDataHandler exportHandler) : IUserDataExporter
{
    public string SectionKey => "questionnaires";

    public async Task<object?> ExportAsync(Guid userId, CancellationToken cancellationToken) =>
        await exportHandler.HandleAsync(userId, cancellationToken);
}

/// <summary>Hard-deletes the client's own submissions (docs/DATA_RETENTION_POLICY.md,
/// "Questionnaires — submissions/answers"). Database-level cascade
/// (<c>QuestionnaireAnswerConfiguration</c>/<c>GuidanceResponseConfiguration</c>/
/// <c>GuidanceFollowUpConfiguration</c>, all <c>OnDelete(DeleteBehavior.Cascade)</c> on
/// <c>QuestionnaireSubmissionId</c>) removes the answers, the Expert's guidance responses, and
/// any guidance follow-up together with the submission — see the policy doc's "Guidance authored
/// by the Expert" section for why that cascade is intentional, not incidental.</summary>
public sealed class QuestionnairesUserDataEraser(DbContext dbContext) : IUserDataEraser
{
    public async Task EraseAsync(Guid userId, CancellationToken cancellationToken)
    {
        var submissions = await dbContext.Set<QuestionnaireSubmission>()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);

        dbContext.Set<QuestionnaireSubmission>().RemoveRange(submissions);
    }
}

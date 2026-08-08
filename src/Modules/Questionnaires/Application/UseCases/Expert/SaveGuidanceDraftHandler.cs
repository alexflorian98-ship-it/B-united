using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Expert;

/// <summary>docs/PROMPT.md §25–28: "Do not overwrite published guidance silently... preserve
/// history via a simple version number." If the latest version is still a draft, this updates
/// it in place; if the latest version is already published, this creates a new draft version
/// instead of touching the published one.</summary>
public sealed class SaveGuidanceDraftHandler(DbContext dbContext)
{
    public async Task<Guid> HandleAsync(SaveGuidanceDraftCommand command, CancellationToken cancellationToken)
    {
        var submissionExists = await dbContext.Set<QuestionnaireSubmission>().AnyAsync(s => s.Id == command.SubmissionId, cancellationToken);
        if (!submissionExists)
        {
            throw new NotFoundAppException("The specified submission does not exist.");
        }

        var latest = await dbContext.Set<GuidanceResponse>()
            .Where(g => g.QuestionnaireSubmissionId == command.SubmissionId)
            .OrderByDescending(g => g.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is not null && latest.PublishedAt is null)
        {
            latest.UpdateDraftBody(command.Body);
            await dbContext.SaveChangesAsync(cancellationToken);
            return latest.Id;
        }

        var nextVersion = (latest?.Version ?? 0) + 1;
        var draft = GuidanceResponse.CreateDraft(command.SubmissionId, command.AuthorUserId, nextVersion, command.Body);
        dbContext.Set<GuidanceResponse>().Add(draft);
        await dbContext.SaveChangesAsync(cancellationToken);

        return draft.Id;
    }
}

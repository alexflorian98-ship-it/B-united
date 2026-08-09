using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Contracts;
using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

/// <summary>P3.42 — every questionnaire now belongs to exactly one Content-owned program.
/// <see cref="CreateQuestionnaireCommand.ProgramId"/> is validated against Content via
/// <see cref="IProgramLookup"/> (a Contracts-only cross-module read, never Content's
/// Domain/Infrastructure — CLAUDE.md), and must reference a published program, mirroring
/// Billing's <c>CreateProgramOfferHandler</c> for consistency across the app.</summary>
public sealed class CreateQuestionnaireHandler(DbContext dbContext, IProgramLookup programLookup)
{
    public async Task<Guid> HandleAsync(CreateQuestionnaireCommand command, CancellationToken cancellationToken)
    {
        var program = await programLookup.GetProgramAsync(command.ProgramId, cancellationToken)
            ?? throw new NotFoundAppException("The specified program does not exist.");

        if (program.Status != ProgramLookupStatus.Published)
        {
            throw new BusinessRuleAppException(
                "QUESTIONNAIRE_PROGRAM_NOT_PUBLISHED",
                "errors.questionnaire.programNotPublished",
                "A questionnaire can only be created for a published program.");
        }

        var questionnaire = Questionnaire.Create(command.ProgramId, command.DefaultLanguage, command.ActorId);
        dbContext.Set<Questionnaire>().Add(questionnaire);

        var translation = QuestionnaireTranslation.Create(questionnaire.Id, command.DefaultLanguage, command.Title, command.Description);
        dbContext.Set<QuestionnaireTranslation>().Add(translation);

        await dbContext.SaveChangesAsync(cancellationToken);

        return questionnaire.Id;
    }
}

using BUnited.Modules.Questionnaires.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed class CreateQuestionnaireHandler(DbContext dbContext)
{
    public async Task<Guid> HandleAsync(CreateQuestionnaireCommand command, CancellationToken cancellationToken)
    {
        var questionnaire = Questionnaire.Create(command.DefaultLanguage, command.ActorId);
        dbContext.Set<Questionnaire>().Add(questionnaire);

        var translation = QuestionnaireTranslation.Create(questionnaire.Id, command.DefaultLanguage, command.Title, command.Description);
        dbContext.Set<QuestionnaireTranslation>().Add(translation);

        await dbContext.SaveChangesAsync(cancellationToken);

        return questionnaire.Id;
    }
}

using BUnited.Modules.Questionnaires.Domain;

namespace BUnited.Modules.Questionnaires.Application.UseCases.Admin;

public sealed record ListQuestionnairesQuery(QuestionnaireStatus? Status, int Page, int PageSize);

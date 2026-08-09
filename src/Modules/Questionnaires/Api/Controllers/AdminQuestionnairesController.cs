using BUnited.Modules.Questionnaires.Application.Dtos;
using BUnited.Modules.Questionnaires.Application.UseCases.Admin;
using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BUnited.Modules.Questionnaires.Api.Controllers;

/// <summary>Questionnaire builder (P4.07): CRUD for questionnaires/questions/options, gated on
/// <c>questionnaire.review</c> per docs/TASKS.md P4.07.a's explicit guidance to reuse that
/// permission rather than introduce a new one — there is no separate "author a questionnaire
/// template" permission in the seeded matrix.</summary>
[ApiController]
[Route("api/v1/admin/questionnaires")]
[Authorize(Policy = WellKnownPermissionKeys.QuestionnaireReview)]
public sealed class AdminQuestionnairesController(
    CreateQuestionnaireHandler createQuestionnaireHandler,
    UpsertQuestionnaireTranslationHandler upsertQuestionnaireTranslationHandler,
    QuestionnaireStatusHandler questionnaireStatusHandler,
    GetQuestionnaireDetailHandler getQuestionnaireDetailHandler,
    ListQuestionnairesHandler listQuestionnairesHandler,
    AddQuestionHandler addQuestionHandler,
    UpsertQuestionTranslationHandler upsertQuestionTranslationHandler,
    DeleteQuestionHandler deleteQuestionHandler,
    ReorderQuestionsHandler reorderQuestionsHandler,
    AddQuestionOptionHandler addQuestionOptionHandler,
    UpsertQuestionOptionTranslationHandler upsertQuestionOptionTranslationHandler,
    DeleteQuestionOptionHandler deleteQuestionOptionHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<QuestionnaireListResult>> List(
        [FromQuery] QuestionnaireStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await listQuestionnairesHandler.HandleAsync(new ListQuestionnairesQuery(status, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{questionnaireId:guid}")]
    public async Task<ActionResult<QuestionnaireDetailDto>> Get(Guid questionnaireId, CancellationToken cancellationToken)
    {
        var result = await getQuestionnaireDetailHandler.HandleAsync(questionnaireId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateQuestionnaireRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateQuestionnaireCommand(request.ProgramId, request.DefaultLanguage, request.Title, request.Description, User.GetUserId());
        var id = await createQuestionnaireHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { questionnaireId = id }, id);
    }

    [HttpPut("{questionnaireId:guid}/translations")]
    public async Task<IActionResult> UpsertTranslation(Guid questionnaireId, UpsertQuestionnaireTranslationRequest request, CancellationToken cancellationToken)
    {
        await upsertQuestionnaireTranslationHandler.HandleAsync(
            new UpsertQuestionnaireTranslationCommand(questionnaireId, request.Language, request.Title, request.Description),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{questionnaireId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid questionnaireId, CancellationToken cancellationToken)
    {
        await questionnaireStatusHandler.PublishAsync(questionnaireId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{questionnaireId:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid questionnaireId, CancellationToken cancellationToken)
    {
        await questionnaireStatusHandler.UnpublishAsync(questionnaireId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{questionnaireId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid questionnaireId, CancellationToken cancellationToken)
    {
        await questionnaireStatusHandler.ArchiveAsync(questionnaireId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{questionnaireId:guid}/questions/reorder")]
    public async Task<IActionResult> ReorderQuestions(Guid questionnaireId, ReorderQuestionsRequest request, CancellationToken cancellationToken)
    {
        await reorderQuestionsHandler.HandleAsync(new ReorderQuestionsCommand(questionnaireId, request.OrderedQuestionIds), User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{questionnaireId:guid}/questions")]
    public async Task<ActionResult<Guid>> AddQuestion(Guid questionnaireId, AddQuestionRequest request, CancellationToken cancellationToken)
    {
        var command = new AddQuestionCommand(questionnaireId, request.Type, request.IsRequired, request.Text, request.HelpText, User.GetUserId());
        var id = await addQuestionHandler.HandleAsync(command, cancellationToken);
        return Ok(id);
    }

    [HttpPut("questions/{questionId:guid}/translations")]
    public async Task<IActionResult> UpsertQuestionTranslation(Guid questionId, UpsertQuestionTranslationRequest request, CancellationToken cancellationToken)
    {
        await upsertQuestionTranslationHandler.HandleAsync(new UpsertQuestionTranslationCommand(questionId, request.Language, request.Text, request.HelpText), cancellationToken);
        return NoContent();
    }

    [HttpDelete("questions/{questionId:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        await deleteQuestionHandler.HandleAsync(questionId, cancellationToken);
        return NoContent();
    }

    [HttpPost("questions/{questionId:guid}/options")]
    public async Task<ActionResult<Guid>> AddQuestionOption(Guid questionId, AddQuestionOptionRequest request, CancellationToken cancellationToken)
    {
        var id = await addQuestionOptionHandler.HandleAsync(new AddQuestionOptionCommand(questionId, request.Value, request.Label), cancellationToken);
        return Ok(id);
    }

    [HttpPut("options/{optionId:guid}/translations")]
    public async Task<IActionResult> UpsertQuestionOptionTranslation(Guid optionId, UpsertQuestionOptionTranslationRequest request, CancellationToken cancellationToken)
    {
        await upsertQuestionOptionTranslationHandler.HandleAsync(new UpsertQuestionOptionTranslationCommand(optionId, request.Language, request.Label), cancellationToken);
        return NoContent();
    }

    [HttpDelete("options/{optionId:guid}")]
    public async Task<IActionResult> DeleteQuestionOption(Guid optionId, CancellationToken cancellationToken)
    {
        await deleteQuestionOptionHandler.HandleAsync(optionId, cancellationToken);
        return NoContent();
    }
}

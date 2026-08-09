using BUnited.Modules.Content.Application.Dtos;
using BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;
using BUnited.Modules.Content.Application.UseCases.Admin.Programs;
using BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;
using BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;
using BUnited.Modules.Content.Application.UseCases.Admin.Sections;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Api.Controllers;

[ApiController]
[Route("api/v1/admin/content")]
[Authorize]
public sealed class AdminContentController(
    CreateProgramHandler createProgramHandler,
    UpsertProgramTranslationHandler upsertProgramTranslationHandler,
    ProgramStatusHandler programStatusHandler,
    GetProgramDetailHandler getProgramDetailHandler,
    ListProgramsHandler listProgramsHandler,
    ReorderSectionsHandler reorderSectionsHandler,
    AddSectionHandler addSectionHandler,
    UpsertSectionTranslationHandler upsertSectionTranslationHandler,
    DeleteSectionHandler deleteSectionHandler,
    ReorderContentItemsHandler reorderContentItemsHandler,
    AddContentItemHandler addContentItemHandler,
    UpsertContentItemTranslationHandler upsertContentItemTranslationHandler,
    DeleteContentItemHandler deleteContentItemHandler,
    AddQuizQuestionHandler addQuizQuestionHandler,
    UpsertQuizQuestionTranslationHandler upsertQuizQuestionTranslationHandler,
    DeleteQuizQuestionHandler deleteQuizQuestionHandler,
    ReorderQuizQuestionsHandler reorderQuizQuestionsHandler,
    AddQuizOptionHandler addQuizOptionHandler,
    UpsertQuizOptionTranslationHandler upsertQuizOptionTranslationHandler,
    DeleteQuizOptionHandler deleteQuizOptionHandler,
    ReorderQuizOptionsHandler reorderQuizOptionsHandler) : ControllerBase
{
    [HttpGet("programs")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentView)]
    public async Task<ActionResult<ProgramListResult>> ListPrograms(
        [FromQuery] ContentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await listProgramsHandler.HandleAsync(new ListProgramsQuery(status, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("programs/{programId:guid}")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentView)]
    public async Task<ActionResult<ProgramDetailDto>> GetProgram(Guid programId, CancellationToken cancellationToken)
    {
        var result = await getProgramDetailHandler.HandleAsync(programId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("programs")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentCreate)]
    public async Task<ActionResult<Guid>> CreateProgram(CreateProgramRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProgramCommand(request.DomainId, request.Slug, request.DefaultLanguage, request.Title, request.ShortDescription, request.Description, User.GetUserId());
        var programId = await createProgramHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetProgram), new { programId }, programId);
    }

    [HttpPut("programs/{programId:guid}/translations")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> UpsertProgramTranslation(Guid programId, UpsertProgramTranslationRequest request, CancellationToken cancellationToken)
    {
        await upsertProgramTranslationHandler.HandleAsync(
            new UpsertProgramTranslationCommand(programId, request.Language, request.Title, request.ShortDescription, request.Description),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("programs/{programId:guid}/publish")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentPublish)]
    public async Task<IActionResult> PublishProgram(Guid programId, CancellationToken cancellationToken)
    {
        await programStatusHandler.PublishAsync(programId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("programs/{programId:guid}/unpublish")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentPublish)]
    public async Task<IActionResult> UnpublishProgram(Guid programId, CancellationToken cancellationToken)
    {
        await programStatusHandler.UnpublishAsync(programId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("programs/{programId:guid}/archive")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentPublish)]
    public async Task<IActionResult> ArchiveProgram(Guid programId, CancellationToken cancellationToken)
    {
        await programStatusHandler.ArchiveAsync(programId, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("programs/{programId:guid}/sections/reorder")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> ReorderSections(Guid programId, ReorderSectionsRequest request, CancellationToken cancellationToken)
    {
        await reorderSectionsHandler.HandleAsync(new ReorderSectionsCommand(programId, request.OrderedSectionIds), cancellationToken);
        return NoContent();
    }

    [HttpPost("programs/{programId:guid}/sections")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<ActionResult<Guid>> AddSection(Guid programId, AddSectionRequest request, CancellationToken cancellationToken)
    {
        var sectionId = await addSectionHandler.HandleAsync(new AddSectionCommand(programId, request.Language, request.Title, request.Description), cancellationToken);
        return Ok(sectionId);
    }

    [HttpPut("sections/{sectionId:guid}/translations")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> UpsertSectionTranslation(Guid sectionId, UpsertSectionTranslationRequest request, CancellationToken cancellationToken)
    {
        await upsertSectionTranslationHandler.HandleAsync(new UpsertSectionTranslationCommand(sectionId, request.Language, request.Title, request.Description), cancellationToken);
        return NoContent();
    }

    [HttpDelete("sections/{sectionId:guid}")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> DeleteSection(Guid sectionId, CancellationToken cancellationToken)
    {
        await deleteSectionHandler.HandleAsync(sectionId, cancellationToken);
        return NoContent();
    }

    [HttpPost("sections/{sectionId:guid}/content-items/reorder")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> ReorderContentItems(Guid sectionId, ReorderContentItemsRequest request, CancellationToken cancellationToken)
    {
        await reorderContentItemsHandler.HandleAsync(new ReorderContentItemsCommand(sectionId, request.OrderedContentItemIds), cancellationToken);
        return NoContent();
    }

    [HttpPost("sections/{sectionId:guid}/content-items")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<ActionResult<Guid>> AddContentItem(Guid sectionId, AddContentItemRequest request, CancellationToken cancellationToken)
    {
        var command = new AddContentItemCommand(sectionId, request.Type, request.IsRequired, request.Language, request.Title, request.Body, request.VideoReference);
        var itemId = await addContentItemHandler.HandleAsync(command, cancellationToken);
        return Ok(itemId);
    }

    [HttpPut("content-items/{contentItemId:guid}/translations")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> UpsertContentItemTranslation(Guid contentItemId, UpsertContentItemTranslationRequest request, CancellationToken cancellationToken)
    {
        await upsertContentItemTranslationHandler.HandleAsync(new UpsertContentItemTranslationCommand(contentItemId, request.Language, request.Title, request.Body), cancellationToken);
        return NoContent();
    }

    [HttpDelete("content-items/{contentItemId:guid}")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> DeleteContentItem(Guid contentItemId, CancellationToken cancellationToken)
    {
        await deleteContentItemHandler.HandleAsync(contentItemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("content-items/{contentItemId:guid}/quiz/questions")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<ActionResult<Guid>> AddQuizQuestion(Guid contentItemId, AddQuizQuestionRequest request, CancellationToken cancellationToken)
    {
        var questionId = await addQuizQuestionHandler.HandleAsync(
            new AddQuizQuestionCommand(contentItemId, request.Language, request.Text), cancellationToken);
        return Ok(questionId);
    }

    [HttpPost("content-items/{contentItemId:guid}/quiz/questions/reorder")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> ReorderQuizQuestions(Guid contentItemId, ReorderQuizQuestionsRequest request, CancellationToken cancellationToken)
    {
        await reorderQuizQuestionsHandler.HandleAsync(
            new ReorderQuizQuestionsCommand(contentItemId, request.OrderedQuizQuestionIds), cancellationToken);
        return NoContent();
    }

    [HttpPut("quiz-questions/{quizQuestionId:guid}/translations")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> UpsertQuizQuestionTranslation(Guid quizQuestionId, UpsertQuizQuestionTranslationRequest request, CancellationToken cancellationToken)
    {
        await upsertQuizQuestionTranslationHandler.HandleAsync(
            new UpsertQuizQuestionTranslationCommand(quizQuestionId, request.Language, request.Text), cancellationToken);
        return NoContent();
    }

    [HttpDelete("quiz-questions/{quizQuestionId:guid}")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> DeleteQuizQuestion(Guid quizQuestionId, CancellationToken cancellationToken)
    {
        await deleteQuizQuestionHandler.HandleAsync(quizQuestionId, cancellationToken);
        return NoContent();
    }

    [HttpPost("quiz-questions/{quizQuestionId:guid}/options")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<ActionResult<Guid>> AddQuizOption(Guid quizQuestionId, AddQuizOptionRequest request, CancellationToken cancellationToken)
    {
        var optionId = await addQuizOptionHandler.HandleAsync(
            new AddQuizOptionCommand(quizQuestionId, request.Language, request.Label, request.IsCorrect), cancellationToken);
        return Ok(optionId);
    }

    [HttpPost("quiz-questions/{quizQuestionId:guid}/options/reorder")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> ReorderQuizOptions(Guid quizQuestionId, ReorderQuizOptionsRequest request, CancellationToken cancellationToken)
    {
        await reorderQuizOptionsHandler.HandleAsync(
            new ReorderQuizOptionsCommand(quizQuestionId, request.OrderedQuizOptionIds), cancellationToken);
        return NoContent();
    }

    [HttpPut("quiz-options/{quizOptionId:guid}/translations")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> UpsertQuizOptionTranslation(Guid quizOptionId, UpsertQuizOptionTranslationRequest request, CancellationToken cancellationToken)
    {
        await upsertQuizOptionTranslationHandler.HandleAsync(
            new UpsertQuizOptionTranslationCommand(quizOptionId, request.Language, request.Label), cancellationToken);
        return NoContent();
    }

    [HttpDelete("quiz-options/{quizOptionId:guid}")]
    [Authorize(Policy = WellKnownPermissionKeys.ContentEdit)]
    public async Task<IActionResult> DeleteQuizOption(Guid quizOptionId, CancellationToken cancellationToken)
    {
        await deleteQuizOptionHandler.HandleAsync(quizOptionId, cancellationToken);
        return NoContent();
    }
}

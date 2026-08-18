using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Application.UseCases.Admin.ContentItems;
using BUnited.Modules.Content.Application.UseCases.Admin.Programs;
using BUnited.Modules.Content.Application.UseCases.Admin.QuizOptions;
using BUnited.Modules.Content.Application.UseCases.Admin.QuizQuestions;
using BUnited.Modules.Content.Application.UseCases.Admin.Sections;
using BUnited.Modules.Content.Application.UseCases.Client;
using BUnited.Modules.Content.Domain;
using BUnited.Modules.Content.Domain.Entities;
using BUnited.Modules.Content.Infrastructure.Video;
using BUnited.Modules.Content.Tests.TestSupport;

namespace BUnited.Modules.Content.Tests.Application;

/// <summary>Covers the new auto-scored Quiz content-item type: admin authoring, the pre-submission
/// read never leaking <c>IsCorrect</c>, server-side grading correctness, cross-program access
/// denial, and tampering rejection (an option id from a different content item's question).</summary>
public sealed class QuizFlowTests
{
    private static readonly Guid ActorId = Guid.NewGuid();

    private static async Task<(TestSupport.TestDbContext Context, Microsoft.Data.Sqlite.SqliteConnection Connection, Guid ProgramId, Guid ContentItemId)> SeedQuizAsync()
    {
        var (connection, context) = TestDbContextFactory.Create();
        var domain = ContentDomain.Create(Guid.NewGuid(), "psychology", 1);
        context.ContentDomains.Add(domain);
        await context.SaveChangesAsync();

        var programId = await new CreateProgramHandler(context).HandleAsync(
            new CreateProgramCommand(domain.Id, "quiz-program", "ro", "Titlu", "Scurt", "Descriere", ActorId), CancellationToken.None);
        var sectionId = await new AddSectionHandler(context).HandleAsync(
            new AddSectionCommand(programId, "ro", "Sectiune", "D"), CancellationToken.None);

        var contentItemId = await new AddContentItemHandler(context, new YouTubeVideoProvider()).HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.Quiz, true, "ro", "Quiz", null, null), CancellationToken.None);

        await new ProgramStatusHandler(context, new FakeAuditLogger(), new RecordingChatRoomProvisioner()).PublishAsync(programId, ActorId, CancellationToken.None);

        return (context, connection, programId, contentItemId);
    }

    private static async Task<(Guid QuestionId, Guid CorrectOptionId, Guid WrongOptionId)> AddQuestionWithTwoOptionsAsync(TestSupport.TestDbContext context, Guid contentItemId, string text = "2+2?")
    {
        var questionId = await new AddQuizQuestionHandler(context).HandleAsync(
            new AddQuizQuestionCommand(contentItemId, "ro", text), CancellationToken.None);
        var correctOptionId = await new AddQuizOptionHandler(context).HandleAsync(
            new AddQuizOptionCommand(questionId, "ro", "4", true), CancellationToken.None);
        var wrongOptionId = await new AddQuizOptionHandler(context).HandleAsync(
            new AddQuizOptionCommand(questionId, "ro", "5", false), CancellationToken.None);
        return (questionId, correctOptionId, wrongOptionId);
    }

    [Fact]
    public async Task Admin_authoring_enforces_exactly_one_correct_option_per_question()
    {
        var (context, connection, _, contentItemId) = await SeedQuizAsync();
        using var _ = connection;
        using var __ = context;

        var (questionId, _, _) = await AddQuestionWithTwoOptionsAsync(context, contentItemId);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            new AddQuizOptionHandler(context).HandleAsync(
                new AddQuizOptionCommand(questionId, "ro", "6", true), CancellationToken.None));
        Assert.Equal("QUIZ_OPTION_ALREADY_HAS_CORRECT_ANSWER", ex.Code);
    }

    [Fact]
    public async Task Reordering_quiz_questions_with_the_wrong_id_set_is_rejected()
    {
        var (context, connection, _, contentItemId) = await SeedQuizAsync();
        using var _ = connection;
        using var __ = context;

        var q1 = await AddQuestionWithTwoOptionsAsync(context, contentItemId, "Q1");
        await AddQuestionWithTwoOptionsAsync(context, contentItemId, "Q2");

var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            new ReorderQuizQuestionsHandler(context).HandleAsync(
                new ReorderQuizQuestionsCommand(contentItemId, [q1.QuestionId]), CancellationToken.None));
        Assert.Equal("QUIZ_QUESTION_REORDER_SET_MISMATCH", ex.Code);
    }

    [Fact]
    public async Task Admin_program_detail_includes_quiz_questions_and_which_option_is_correct()
    {
        var (context, connection, programId, contentItemId) = await SeedQuizAsync();
        using var _ = connection;
        using var __ = context;

        var (questionId, correctOptionId, wrongOptionId) = await AddQuestionWithTwoOptionsAsync(context, contentItemId);

        var detail = await new GetProgramDetailHandler(context).HandleAsync(programId, CancellationToken.None);

        var quizItem = Assert.Single(detail.Sections.Single().Items);
        var question = Assert.Single(quizItem.QuizQuestions!);
        Assert.Equal(questionId, question.Id);
        Assert.Equal(2, question.Options.Count);
        Assert.Contains(question.Options, o => o.Id == correctOptionId && o.IsCorrect);
        Assert.Contains(question.Options, o => o.Id == wrongOptionId && !o.IsCorrect);
    }

    [Fact]
    public async Task Published_quiz_detail_never_exposes_which_option_is_correct()
    {
        var (context, connection, _, contentItemId) = await SeedQuizAsync();
        using var _ = connection;
        using var __ = context;

        await AddQuestionWithTwoOptionsAsync(context, contentItemId);

        var detailHandler = new GetPublishedProgramDetailHandler(context, new FakeProgramOfferLookup(), new FakeProgramAccessContext());
        var detail = await detailHandler.HandleAsync("quiz-program", "ro", null, CancellationToken.None);

        var quizItem = Assert.Single(detail.Sections.Single().Items);
        Assert.Equal("Quiz", quizItem.Type);
        var question = Assert.Single(quizItem.QuizQuestions!);
        Assert.Equal(2, question.Options.Count);
        // ClientQuizOptionDto only has Id/SortOrder/Label — there is no IsCorrect property to
        // assert against, which is itself the guarantee: it cannot leak because the DTO shape
        // has no field for it. Confirm the DTO shape directly via reflection as a regression
        // guard in case a future edit adds one back.
        var optionType = question.Options[0].GetType();
        Assert.DoesNotContain(optionType.GetProperties(), p => p.Name.Contains("Correct", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Submitting_a_quiz_grades_correctly_for_mixed_correct_and_incorrect_answers()
    {
        var (context, connection, programId, contentItemId) = await SeedQuizAsync();
        using var _ = connection;
        using var __ = context;

        var q1 = await AddQuestionWithTwoOptionsAsync(context, contentItemId, "Q1");
        var q2 = await AddQuestionWithTwoOptionsAsync(context, contentItemId, "Q2");

        var userId = Guid.NewGuid();
        var accessContext = new FakeProgramAccessContext();
        accessContext.GrantAccess(userId, programId);
        var handler = new SubmitQuizAttemptHandler(context, accessContext, TimeProvider.System);

        var result = await handler.HandleAsync(
            new SubmitQuizAttemptCommand(contentItemId, userId,
            [
                new QuizAnswerInput(q1.QuestionId, q1.CorrectOptionId),
                new QuizAnswerInput(q2.QuestionId, q2.WrongOptionId),
            ]),
            CancellationToken.None);

        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(1, result.CorrectCount);
        Assert.Contains(result.PerQuestionResults, r => r.QuestionId == q1.QuestionId && r.WasCorrect && r.CorrectOptionId == q1.CorrectOptionId);
        Assert.Contains(result.PerQuestionResults, r => r.QuestionId == q2.QuestionId && !r.WasCorrect && r.CorrectOptionId == q2.CorrectOptionId);
    }

    [Fact]
    public async Task Submitting_a_quiz_for_a_program_the_caller_does_not_own_is_denied()
    {
        var (context, connection, _, contentItemId) = await SeedQuizAsync();
        using var _ = connection;
        using var __ = context;

        var q1 = await AddQuestionWithTwoOptionsAsync(context, contentItemId);

        var userId = Guid.NewGuid();
        var accessContext = new FakeProgramAccessContext(); // no access granted
        var handler = new SubmitQuizAttemptHandler(context, accessContext, TimeProvider.System);

        await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            handler.HandleAsync(
                new SubmitQuizAttemptCommand(contentItemId, userId, [new QuizAnswerInput(q1.QuestionId, q1.CorrectOptionId)]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Submitting_an_option_id_belonging_to_a_different_content_items_question_is_rejected()
    {
        var (context, connection, programId, contentItemId) = await SeedQuizAsync();
        using var _ = connection;
        using var __ = context;

        var q1 = await AddQuestionWithTwoOptionsAsync(context, contentItemId);

        // A second, unrelated quiz content item with its own question/options.
        var sectionId = await new AddSectionHandler(context).HandleAsync(
            new AddSectionCommand(programId, "ro", "Alta sectiune", "D"), CancellationToken.None);
        var otherContentItemId = await new AddContentItemHandler(context, new YouTubeVideoProvider()).HandleAsync(
            new AddContentItemCommand(sectionId, ContentItemType.Quiz, true, "ro", "Alt quiz", null, null), CancellationToken.None);
        var other = await AddQuestionWithTwoOptionsAsync(context, otherContentItemId, "Other quiz question");

        var userId = Guid.NewGuid();
        var accessContext = new FakeProgramAccessContext();
        accessContext.GrantAccess(userId, programId);
        var handler = new SubmitQuizAttemptHandler(context, accessContext, TimeProvider.System);

        // Submitting the ORIGINAL quiz's question id but with an option id stolen from the OTHER
        // quiz's question must be rejected outright, not silently scored.
        await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            handler.HandleAsync(
                new SubmitQuizAttemptCommand(contentItemId, userId, [new QuizAnswerInput(q1.QuestionId, other.CorrectOptionId)]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Retaking_a_quiz_records_a_new_attempt_without_losing_the_previous_one()
    {
        var (context, connection, programId, contentItemId) = await SeedQuizAsync();
        using var _ = connection;
        using var __ = context;

        var q1 = await AddQuestionWithTwoOptionsAsync(context, contentItemId);

        var userId = Guid.NewGuid();
        var accessContext = new FakeProgramAccessContext();
        accessContext.GrantAccess(userId, programId);
        var handler = new SubmitQuizAttemptHandler(context, accessContext, TimeProvider.System);

        await handler.HandleAsync(
            new SubmitQuizAttemptCommand(contentItemId, userId, [new QuizAnswerInput(q1.QuestionId, q1.WrongOptionId)]), CancellationToken.None);
        await handler.HandleAsync(
            new SubmitQuizAttemptCommand(contentItemId, userId, [new QuizAnswerInput(q1.QuestionId, q1.CorrectOptionId)]), CancellationToken.None);

        var attempts = context.QuizAttempts.Where(a => a.UserId == userId && a.ContentItemId == contentItemId).ToList();
        Assert.Equal(2, attempts.Count);
        Assert.Contains(attempts, a => a.CorrectCount == 0);
        Assert.Contains(attempts, a => a.CorrectCount == 1);
    }
}

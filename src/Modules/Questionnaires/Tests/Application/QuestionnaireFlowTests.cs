using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Questionnaires.Application.UseCases.Admin;
using BUnited.Modules.Questionnaires.Application.UseCases.Client;
using BUnited.Modules.Questionnaires.Application.UseCases.Expert;
using BUnited.Modules.Questionnaires.Tests.TestSupport;

namespace BUnited.Modules.Questionnaires.Tests.Application;

public sealed class QuestionnaireFlowTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    private sealed record Fixture(
        TestDbContext DbContext,
        FakeConsentContext Consent,
        FakeUserLookup UserLookup,
        FakeAuditLogger AuditLogger,
        FakeNotificationSender NotificationSender);

    private static Fixture CreateFixture(out IDisposable connection)
    {
        var (conn, context) = TestDbContextFactory.Create();
        connection = conn;
        return new Fixture(context, new FakeConsentContext(), new FakeUserLookup(), new FakeAuditLogger(), new FakeNotificationSender());
    }

    private static async Task<(Guid QuestionnaireId, Guid TextQuestionId, Guid ChoiceQuestionId)> AuthorAndPublishQuestionnaireAsync(Fixture fx)
    {
        var expertId = Guid.NewGuid();
        var createHandler = new CreateQuestionnaireHandler(fx.DbContext);
        var questionnaireId = await createHandler.HandleAsync(
            new CreateQuestionnaireCommand("ro", "Chestionar inițial", "Descriere", expertId), CancellationToken.None);

        var addQuestionHandler = new AddQuestionHandler(fx.DbContext);
        var textQuestionId = await addQuestionHandler.HandleAsync(
            new AddQuestionCommand(questionnaireId, "Text", true, "Ce te aduce aici?", null, expertId), CancellationToken.None);
        var choiceQuestionId = await addQuestionHandler.HandleAsync(
            new AddQuestionCommand(questionnaireId, "SingleChoice", true, "Cum te simți?", null, expertId), CancellationToken.None);

        var addOptionHandler = new AddQuestionOptionHandler(fx.DbContext);
        await addOptionHandler.HandleAsync(new AddQuestionOptionCommand(choiceQuestionId, "good", "Bine"), CancellationToken.None);
        await addOptionHandler.HandleAsync(new AddQuestionOptionCommand(choiceQuestionId, "bad", "Rău"), CancellationToken.None);

        var statusHandler = new QuestionnaireStatusHandler(fx.DbContext);
        await statusHandler.PublishAsync(questionnaireId, expertId, CancellationToken.None);

        return (questionnaireId, textQuestionId, choiceQuestionId);
    }

    [Fact]
    public async Task Full_flow_draft_to_published_guidance_with_followup_succeeds_end_to_end()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;

        var (questionnaireId, textQuestionId, choiceQuestionId) = await AuthorAndPublishQuestionnaireAsync(fx);
        var clientId = Guid.NewGuid();
        var expertId = Guid.NewGuid();

        // Client starts, answers, submits.
        await new RecordQuestionnaireConsentHandler(fx.Consent).HandleAsync(clientId, CancellationToken.None);
        var submissionId = await new StartOrResumeSubmissionHandler(fx.DbContext, fx.Consent).HandleAsync(clientId, questionnaireId, CancellationToken.None);

        var clientView = await new GetClientQuestionnaireHandler(fx.DbContext).HandleAsync(questionnaireId, "ro", CancellationToken.None);
        Assert.Equal(2, clientView.Questions.Count);
        Assert.Equal(2, clientView.Questions.Single(q => q.Id == choiceQuestionId).Options.Count);

        await new SaveDraftAnswersHandler(fx.DbContext, Clock).HandleAsync(
            new SaveDraftAnswersCommand(clientId, submissionId, [new AnswerInput(textQuestionId, "Vreau ghidare."), new AnswerInput(choiceQuestionId, "good")]),
            CancellationToken.None);

        await new SubmitQuestionnaireHandler(fx.DbContext, Clock, fx.AuditLogger).HandleAsync(clientId, submissionId, CancellationToken.None);
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "questionnaire.submitted");

        // Expert reviews the queue and opens the submission.
        var queue = await new GetQueueHandler(fx.DbContext, fx.UserLookup, Clock).HandleAsync(CancellationToken.None);
        Assert.Single(queue);
        Assert.Equal(submissionId, queue[0].SubmissionId);

        var detail = await new GetSubmissionDetailForExpertHandler(fx.DbContext, fx.UserLookup, Clock, fx.AuditLogger)
            .HandleAsync(submissionId, expertId, CancellationToken.None);
        Assert.Equal(2, detail.Answers.Count);
        var choiceAnswer = detail.Answers.Single(a => a.QuestionId == choiceQuestionId);
        Assert.Equal(["Bine"], choiceAnswer.AnswerLabels);
        Assert.Contains(fx.AuditLogger.Entries, e => e.Action == "questionnaire.read");

        // Expert writes and publishes guidance.
        var guidanceId = await new SaveGuidanceDraftHandler(fx.DbContext).HandleAsync(
            new SaveGuidanceDraftCommand(submissionId, "Iată recomandarea mea inițială.", expertId), CancellationToken.None);
        await new PublishGuidanceHandler(fx.DbContext, Clock, fx.AuditLogger, fx.UserLookup, fx.NotificationSender)
            .HandleAsync(guidanceId, expertId, CancellationToken.None);

        Assert.Contains(fx.NotificationSender.Sent, s => s.Type == Notifications.Contracts.NotificationType.GuidancePublished);

        // Client reads the published guidance and asks one follow-up.
        var guidance = await new GetGuidanceHandler(fx.DbContext).HandleAsync(clientId, submissionId, CancellationToken.None);
        Assert.NotNull(guidance);
        Assert.Equal(1, guidance!.Version);

        await new SubmitFollowUpHandler(fx.DbContext).HandleAsync(
            new SubmitFollowUpCommand(clientId, guidance.Id, "Poți detalia puțin?"), CancellationToken.None);

        // A second follow-up on the same guidance is rejected.
        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            new SubmitFollowUpHandler(fx.DbContext).HandleAsync(
                new SubmitFollowUpCommand(clientId, guidance.Id, "O a doua întrebare"), CancellationToken.None));
        Assert.Equal("GUIDANCE_FOLLOWUP_ALREADY_EXISTS", ex.Code);

        // Expert answers the follow-up.
        var refreshedGuidance = await new GetGuidanceHandler(fx.DbContext).HandleAsync(clientId, submissionId, CancellationToken.None);
        await new AnswerFollowUpHandler(fx.DbContext, Clock).HandleAsync(
            new AnswerFollowUpCommand(refreshedGuidance!.FollowUp!.Id, "Sigur, iată mai multe detalii."), CancellationToken.None);

        var finalGuidance = await new GetGuidanceHandler(fx.DbContext).HandleAsync(clientId, submissionId, CancellationToken.None);
        Assert.Equal("Sigur, iată mai multe detalii.", finalGuidance!.FollowUp!.Answer);
    }

    [Fact]
    public async Task Submitting_without_required_answers_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;

        var (questionnaireId, _, _) = await AuthorAndPublishQuestionnaireAsync(fx);
        var clientId = Guid.NewGuid();

        await new RecordQuestionnaireConsentHandler(fx.Consent).HandleAsync(clientId, CancellationToken.None);
        var submissionId = await new StartOrResumeSubmissionHandler(fx.DbContext, fx.Consent).HandleAsync(clientId, questionnaireId, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            new SubmitQuestionnaireHandler(fx.DbContext, Clock, fx.AuditLogger).HandleAsync(clientId, submissionId, CancellationToken.None));
        Assert.Equal("QUESTIONNAIRE_REQUIRED_ANSWERS_MISSING", ex.Code);
    }

    [Fact]
    public async Task Starting_without_consent_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;

        var (questionnaireId, _, _) = await AuthorAndPublishQuestionnaireAsync(fx);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            new StartOrResumeSubmissionHandler(fx.DbContext, fx.Consent).HandleAsync(Guid.NewGuid(), questionnaireId, CancellationToken.None));
        Assert.Equal("QUESTIONNAIRE_CONSENT_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task A_users_submission_is_invisible_to_another_user()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;

        var (questionnaireId, _, _) = await AuthorAndPublishQuestionnaireAsync(fx);
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await new RecordQuestionnaireConsentHandler(fx.Consent).HandleAsync(ownerId, CancellationToken.None);
        var submissionId = await new StartOrResumeSubmissionHandler(fx.DbContext, fx.Consent).HandleAsync(ownerId, questionnaireId, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            new GetMySubmissionHandler(fx.DbContext).HandleAsync(otherUserId, submissionId, CancellationToken.None));
    }

    [Fact]
    public async Task Starting_the_same_questionnaire_twice_returns_the_same_draft_submission()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;

        var (questionnaireId, _, _) = await AuthorAndPublishQuestionnaireAsync(fx);
        var clientId = Guid.NewGuid();
        await new RecordQuestionnaireConsentHandler(fx.Consent).HandleAsync(clientId, CancellationToken.None);

        var handler = new StartOrResumeSubmissionHandler(fx.DbContext, fx.Consent);
        var first = await handler.HandleAsync(clientId, questionnaireId, CancellationToken.None);
        var second = await handler.HandleAsync(clientId, questionnaireId, CancellationToken.None);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Reordering_questions_with_the_wrong_id_set_is_rejected()
    {
        var fx = CreateFixture(out var connection);
        using var _ = connection;

        var (questionnaireId, textQuestionId, _) = await AuthorAndPublishQuestionnaireAsync(fx);

        var ex = await Assert.ThrowsAsync<BusinessRuleAppException>(() =>
            new ReorderQuestionsHandler(fx.DbContext).HandleAsync(
                new ReorderQuestionsCommand(questionnaireId, [textQuestionId]), Guid.NewGuid(), CancellationToken.None));
        Assert.Equal("QUESTION_REORDER_SET_MISMATCH", ex.Code);
    }
}

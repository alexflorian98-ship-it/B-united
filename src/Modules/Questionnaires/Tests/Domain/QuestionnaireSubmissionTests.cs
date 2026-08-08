using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;

namespace BUnited.Modules.Questionnaires.Tests.Domain;

public sealed class QuestionnaireSubmissionTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_submission_starts_as_draft_with_no_timestamps_set()
    {
        var submission = QuestionnaireSubmission.Start(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(QuestionnaireSubmissionStatus.Draft, submission.Status);
        Assert.Null(submission.StartedAt);
        Assert.Null(submission.SubmittedAt);
    }

    [Fact]
    public void MarkStarted_is_idempotent()
    {
        var submission = QuestionnaireSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        submission.MarkStarted(UtcNow);
        submission.MarkStarted(UtcNow.AddHours(1));

        Assert.Equal(UtcNow, submission.StartedAt);
    }

    [Fact]
    public void Submit_transitions_draft_to_submitted_and_stamps_submitted_at()
    {
        var submission = QuestionnaireSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        submission.Submit(UtcNow);

        Assert.Equal(QuestionnaireSubmissionStatus.Submitted, submission.Status);
        Assert.Equal(UtcNow, submission.SubmittedAt);
    }

    [Fact]
    public void Submit_twice_is_rejected()
    {
        var submission = QuestionnaireSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        submission.Submit(UtcNow);

        Assert.Throws<InvalidOperationException>(() => submission.Submit(UtcNow));
    }

    [Fact]
    public void MarkAnswered_requires_submitted_status()
    {
        var submission = QuestionnaireSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => submission.MarkAnswered(UtcNow));
    }

    [Fact]
    public void Operational_timestamps_are_never_overwritten_once_set()
    {
        var submission = QuestionnaireSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        submission.MarkAssigned(UtcNow);
        submission.MarkAssigned(UtcNow.AddDays(1));

        Assert.Equal(UtcNow, submission.AssignedAt);
    }
}

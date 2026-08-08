using BUnited.Modules.Questionnaires.Domain.Entities;

namespace BUnited.Modules.Questionnaires.Tests.Domain;

public sealed class GuidanceFollowUpTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_followup_has_no_answer()
    {
        var followUp = GuidanceFollowUp.Ask(Guid.NewGuid(), "Can you clarify point 2?");
        Assert.Null(followUp.Answer);
        Assert.Null(followUp.AnsweredAt);
    }

    [Fact]
    public void Respond_sets_answer_and_answered_at()
    {
        var followUp = GuidanceFollowUp.Ask(Guid.NewGuid(), "Question");
        followUp.Respond("Here is the clarification.", UtcNow);

        Assert.Equal("Here is the clarification.", followUp.Answer);
        Assert.Equal(UtcNow, followUp.AnsweredAt);
    }

    [Fact]
    public void Responding_twice_is_rejected()
    {
        var followUp = GuidanceFollowUp.Ask(Guid.NewGuid(), "Question");
        followUp.Respond("First answer", UtcNow);

        Assert.Throws<InvalidOperationException>(() => followUp.Respond("Second answer", UtcNow.AddMinutes(1)));
    }
}

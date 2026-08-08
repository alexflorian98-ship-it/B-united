using BUnited.Modules.Questionnaires.Domain;
using BUnited.Modules.Questionnaires.Domain.Entities;

namespace BUnited.Modules.Questionnaires.Tests.Domain;

public sealed class QuestionnaireTests
{
    [Fact]
    public void New_questionnaire_starts_as_draft()
    {
        var questionnaire = Questionnaire.Create("ro", Guid.NewGuid());
        Assert.Equal(QuestionnaireStatus.Draft, questionnaire.Status);
    }

    [Fact]
    public void Publish_moves_draft_to_published()
    {
        var questionnaire = Questionnaire.Create("ro", Guid.NewGuid());
        questionnaire.Publish(Guid.NewGuid());
        Assert.Equal(QuestionnaireStatus.Published, questionnaire.Status);
    }

    [Fact]
    public void Unpublish_requires_published_status()
    {
        var questionnaire = Questionnaire.Create("ro", Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => questionnaire.Unpublish(Guid.NewGuid()));
    }

    [Fact]
    public void Archived_questionnaire_cannot_be_republished()
    {
        var questionnaire = Questionnaire.Create("ro", Guid.NewGuid());
        questionnaire.Archive(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => questionnaire.Publish(Guid.NewGuid()));
    }
}

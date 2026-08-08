using BUnited.Modules.Progress.Domain;
using BUnited.Modules.Progress.Domain.Entities;

namespace BUnited.Modules.Progress.Tests.Domain;

public sealed class SectionProgressTests
{
    [Fact]
    public void No_completed_items_is_not_started()
    {
        var section = SectionProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        section.Recalculate(0, 3);

        Assert.Equal(ContentProgressStatus.NotStarted, section.Status);
    }

    [Fact]
    public void Some_but_not_all_completed_items_is_in_progress()
    {
        var section = SectionProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        section.Recalculate(1, 3);

        Assert.Equal(ContentProgressStatus.InProgress, section.Status);
    }

    [Fact]
    public void All_items_completed_is_completed()
    {
        var section = SectionProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        section.Recalculate(3, 3);

        Assert.Equal(ContentProgressStatus.Completed, section.Status);
    }

    [Fact]
    public void Recalculation_is_deterministic_for_the_same_inputs()
    {
        var section = SectionProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        section.Recalculate(2, 3);
        var firstStatus = section.Status;
        section.Recalculate(2, 3);

        Assert.Equal(firstStatus, section.Status);
        Assert.Equal(ContentProgressStatus.InProgress, section.Status);
    }
}

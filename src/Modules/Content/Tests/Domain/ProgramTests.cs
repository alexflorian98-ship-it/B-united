using BUnited.Modules.Content.Domain;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Tests.Domain;

public sealed class ProgramTests
{
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public void New_program_starts_as_draft()
    {
        var program = Program.Create(Guid.NewGuid(), "test-program", "ro", ActorId);

        Assert.Equal(ContentStatus.Draft, program.Status);
    }

    [Fact]
    public void Draft_can_be_published()
    {
        var program = Program.Create(Guid.NewGuid(), "test-program", "ro", ActorId);

        program.Publish(ActorId);

        Assert.Equal(ContentStatus.Published, program.Status);
    }

    [Fact]
    public void Published_can_be_unpublished_back_to_draft()
    {
        var program = Program.Create(Guid.NewGuid(), "test-program", "ro", ActorId);
        program.Publish(ActorId);

        program.Unpublish(ActorId);

        Assert.Equal(ContentStatus.Draft, program.Status);
    }

    [Fact]
    public void Draft_cannot_be_unpublished()
    {
        var program = Program.Create(Guid.NewGuid(), "test-program", "ro", ActorId);

        Assert.Throws<InvalidOperationException>(() => program.Unpublish(ActorId));
    }

    [Fact]
    public void Archived_program_cannot_be_published_again()
    {
        var program = Program.Create(Guid.NewGuid(), "test-program", "ro", ActorId);
        program.Archive(ActorId);

        Assert.Throws<InvalidOperationException>(() => program.Publish(ActorId));
    }

    [Fact]
    public void Archived_program_cannot_be_archived_again()
    {
        var program = Program.Create(Guid.NewGuid(), "test-program", "ro", ActorId);
        program.Archive(ActorId);

        Assert.Throws<InvalidOperationException>(() => program.Archive(ActorId));
    }

    [Fact]
    public void A_draft_can_be_archived_directly()
    {
        var program = Program.Create(Guid.NewGuid(), "test-program", "ro", ActorId);

        program.Archive(ActorId);

        Assert.Equal(ContentStatus.Archived, program.Status);
    }
}

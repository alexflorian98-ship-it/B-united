using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed class CreateProgramHandler(DbContext dbContext)
{
    public async Task<Guid> HandleAsync(CreateProgramCommand command, CancellationToken cancellationToken)
    {
        var domainExists = await dbContext.Set<ContentDomain>().AnyAsync(d => d.Id == command.DomainId, cancellationToken);
        if (!domainExists)
        {
            throw new NotFoundAppException("The specified content domain does not exist.");
        }

        var slugTaken = await dbContext.Set<Program>().AnyAsync(p => p.Slug == command.Slug, cancellationToken);
        if (slugTaken)
        {
            throw new BusinessRuleAppException("PROGRAM_SLUG_TAKEN", "errors.program.slugTaken", "A program with this slug already exists.");
        }

        var program = Program.Create(command.DomainId, command.Slug, command.DefaultLanguage, command.ActorId);
        dbContext.Set<Program>().Add(program);

        var translation = ProgramTranslation.Create(program.Id, command.DefaultLanguage, command.Title, command.ShortDescription, command.Description);
        dbContext.Set<ProgramTranslation>().Add(translation);

        await dbContext.SaveChangesAsync(cancellationToken);

        return program.Id;
    }
}

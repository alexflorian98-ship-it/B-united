using BUnited.BuildingBlocks.Application.Errors;
using BUnited.Modules.Content.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Program = BUnited.Modules.Content.Domain.Entities.Program;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed class UpsertProgramTranslationHandler(DbContext dbContext)
{
    public async Task HandleAsync(UpsertProgramTranslationCommand command, CancellationToken cancellationToken)
    {
        var programExists = await dbContext.Set<Program>().AnyAsync(p => p.Id == command.ProgramId, cancellationToken);
        if (!programExists)
        {
            throw new NotFoundAppException("The specified program does not exist.");
        }

        var translation = await dbContext.Set<ProgramTranslation>()
            .SingleOrDefaultAsync(t => t.ProgramId == command.ProgramId && t.Language == command.Language, cancellationToken);

        if (translation is null)
        {
            dbContext.Set<ProgramTranslation>().Add(
                ProgramTranslation.Create(command.ProgramId, command.Language, command.Title, command.ShortDescription, command.Description));
        }
        else
        {
            translation.Update(command.Title, command.ShortDescription, command.Description);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using BUnited.Modules.Content.Domain;

namespace BUnited.Modules.Content.Application.UseCases.Admin.Programs;

public sealed record ListProgramsQuery(ContentStatus? Status, int Page, int PageSize);

namespace BUnited.Modules.Events.Application.UseCases.Admin;

public sealed record SetEventProgramsRequest(IReadOnlyList<Guid> ProgramIds);

public sealed record SetEventProgramsCommand(Guid EventId, IReadOnlyList<Guid> ProgramIds, Guid UpdatedBy);

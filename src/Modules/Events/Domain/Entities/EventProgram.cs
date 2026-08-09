namespace BUnited.Modules.Events.Domain.Entities;

/// <summary>docs/TASKS.md P3.43.b — many-to-many association between an <see cref="Event"/> and
/// the Content-owned program(s) that gate its registration. <see cref="ProgramId"/> is an opaque
/// <see cref="Guid"/> with no FK constraint, matching <c>Questionnaire.ProgramId</c>'s convention
/// (CLAUDE.md: never reference another module's Domain or Infrastructure layer).
///
/// Zero associated rows for a given <see cref="EventId"/> means the event is public-to-all-
/// authenticated-users (today's original behavior, unchanged) — <see cref="Application.UseCases.Client.RegisterForEventHandler"/>
/// only gates registration when at least one row exists, requiring ownership of at least one of
/// the associated programs.</summary>
public sealed class EventProgram
{
    private EventProgram()
    {
    }

    public static EventProgram Create(Guid eventId, Guid programId) =>
        new()
        {
            EventId = eventId,
            ProgramId = programId,
        };

    public Guid EventId { get; private set; }

    public Guid ProgramId { get; private set; }
}

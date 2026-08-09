using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Chat.Domain.Entities;

/// <summary>docs/PROMPT.md §33-34, docs/TASKS.md P3.43.a. Was originally modeled as a fixed
/// 6-member enum (see git history) since there was no create/edit/delete-room use case — that
/// changed once rooms had to be scoped to a specific Content-owned program: "a room must
/// reference a program, and only clients entitled to that program may read or post in it. A
/// program may have zero or more predefined rooms; clients cannot create rooms" (still true here —
/// only admin-managed factories/mutators exist, no client-facing create endpoint).
///
/// <see cref="ProgramId"/> is an opaque <see cref="Guid"/> with no FK constraint (cross-module
/// boundary — CLAUDE.md), and is nullable specifically to represent the 6 legacy rooms that
/// predate program-scoping: they were deactivated (<see cref="IsActive"/> = false) rather than
/// mapped to an invented program association (a confirmed product decision — see the migration
/// that introduces this table), and since an inactive room is never reachable through the
/// entitlement check or room discovery anyway, a null <see cref="ProgramId"/> on those rows is
/// inert. Every room created after this migration goes through <see cref="Create"/>, which
/// requires a real <see cref="ProgramId"/>.</summary>
public sealed class ChatRoom : IAuditableEntity
{
    private ChatRoom()
    {
    }

    public static ChatRoom Create(Guid programId, string key, string name, Guid? createdBy) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProgramId = programId,
            Key = key,
            Name = name,
            IsActive = true,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };

    public Guid Id { get; private set; }

    public Guid? ProgramId { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; private set; }

    public void Rename(string name, Guid updatedBy)
    {
        Name = name;
        UpdatedBy = updatedBy;
    }

    public void Activate(Guid updatedBy)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(Guid updatedBy)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
    }
}

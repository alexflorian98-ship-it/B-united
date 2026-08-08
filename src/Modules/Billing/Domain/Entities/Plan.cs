using BUnited.BuildingBlocks.Domain;

namespace BUnited.Modules.Billing.Domain.Entities;

/// <summary>docs/PROMPT.md §15/§63.</summary>
public sealed class Plan : IAuditableEntity
{
    private Plan()
    {
    }

    public static Plan Create(string name, string description) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsActive = true,
        };

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}

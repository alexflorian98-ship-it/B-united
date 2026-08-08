namespace BUnited.Modules.Identity.Domain.Entities;

/// <summary>
/// A single, stable permission key (e.g. <c>content.publish</c>). See
/// <c>WellKnownPermissions</c> for the canonical key list — do not construct ad hoc keys at
/// call sites (docs/DEVELOPMENT_INSTRUCTIONS.md §3).
/// </summary>
public sealed class Permission
{
    private Permission()
    {
    }

    public Permission(Guid id, string key, string description)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Permission key is required.", nameof(key));
        }

        Id = id;
        Key = key;
        Description = description;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;
}

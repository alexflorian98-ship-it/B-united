namespace BUnited.Modules.Identity.Domain.Entities;

/// <summary>
/// A convenience grouping of permissions (§14). Authorization decisions always test the
/// permission claim, never the role name — see <c>WellKnownPermissions</c> and
/// docs/DEVELOPMENT_INSTRUCTIONS.md's ban on role-string checks.
/// </summary>
public sealed class Role
{
    private readonly List<RolePermission> _rolePermissions = [];

    private Role()
    {
    }

    public Role(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(name));
        }

        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions;

    public void Grant(Guid permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
        {
            return;
        }

        _rolePermissions.Add(new RolePermission(Id, permissionId));
    }
}

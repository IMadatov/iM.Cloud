namespace iM.Cloud.Domain.Entities;

public class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission() { }

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role id is required.", nameof(roleId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException("Permission id is required.", nameof(permissionId));

        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };
    }
}

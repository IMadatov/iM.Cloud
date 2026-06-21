namespace iM.Cloud.Domain.Entities;

public class UserPermission
{
    public Guid UserId { get; private set; }
    public Guid PermissionId { get; private set; }

    private UserPermission() { }

    public static UserPermission Create(Guid userId, Guid permissionId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException("Permission id is required.", nameof(permissionId));

        return new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId
        };
    }
}

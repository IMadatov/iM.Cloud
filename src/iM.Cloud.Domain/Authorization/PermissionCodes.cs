namespace iM.Cloud.Domain.Authorization;

public static class PermissionCodes
{
    public const string UsersCreate = "users.create";
    public const string UsersRead = "users.read";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";
    public const string RolesManage = "roles.manage";
    public const string PermissionsAssign = "permissions.assign";
    public const string GroupsManage = "groups.manage";
    public const string GroupsRead = "groups.read";

    public static IReadOnlyList<string> All { get; } =
    [
        UsersCreate,
        UsersRead,
        UsersUpdate,
        UsersDelete,
        RolesManage,
        PermissionsAssign,
        GroupsManage,
        GroupsRead
    ];
}

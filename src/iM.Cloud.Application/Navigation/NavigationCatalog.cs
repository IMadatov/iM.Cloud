using iM.Cloud.Domain.Authorization;

namespace iM.Cloud.Application.Navigation;

public sealed record NavigationCatalogEntry(
    string Key,
    string Label,
    string Icon,
    string Path,
    int Order,
    string? RequiredPermission = null);

public static class NavigationCatalog
{
    public static IReadOnlyList<NavigationCatalogEntry> Items { get; } =
    [
        new("home", "nav.home", "pi pi-home", "/", 0),
        new("files", "nav.files", "pi pi-folder", "/files", 5),
        new("my_groups", "nav.my_groups", "pi pi-users", "/groups", 7),
        new("users", "nav.users", "pi pi-users", "/admin/users", 10, PermissionCodes.UsersRead),
        new("roles", "nav.roles", "pi pi-shield", "/admin/roles", 20, PermissionCodes.RolesManage),
        new("groups", "nav.groups", "pi pi-sitemap", "/admin/groups", 30, PermissionCodes.GroupsRead),
        new("permissions", "nav.permissions", "pi pi-key", "/admin/permissions", 40, PermissionCodes.RolesManage),
    ];
}

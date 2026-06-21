using iM.Cloud.Domain.Dtos.Permissions;
using iM.Cloud.Domain.Entities;

namespace iM.Cloud.Domain.Mappings;

public static class PermissionMappings
{
    public static PermissionDetailsDto ToDetailsDto(Permission permission) => new()
    {
        Id = permission.Id,
        Code = permission.Code,
        Name = permission.Name,
        Description = permission.Description,
        Active = permission.Active
    };
}

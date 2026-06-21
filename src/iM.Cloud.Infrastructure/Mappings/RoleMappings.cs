using iM.Cloud.Infrastructure.Dtos.Roles;
using iM.Cloud.Infrastructure.Identity;

namespace iM.Cloud.Infrastructure.Mappings;

public static class RoleMappings
{
    public static RoleDetailsDto ToDetailsDto(ApplicationRole role) => new()
    {
        Id = role.Id,
        Name = role.Name ?? string.Empty,
        Description = role.Description,
        Active = role.Active
    };

    public static void ApplyDetails(ApplicationRole role, RoleDetailsDto dto)
    {
        role.Name = dto.Name.Trim();
        role.NormalizedName = dto.Name.Trim().ToUpperInvariant();
        role.Description = dto.Description?.Trim();
        role.Active = dto.Active;
        role.LastModifiedDate = DateTime.UtcNow;
    }
}

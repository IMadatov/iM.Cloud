using BaseCrud.Abstractions.Entities;
using iM.Cloud.Infrastructure.Dtos.Users;
using iM.Cloud.Infrastructure.Identity;

namespace iM.Cloud.Infrastructure.Mappings;

public static class UserMappings
{
    public static UserDetailsDto ToDetailsDto(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        DisplayName = user.DisplayName,
        Active = user.Active,
        CreatedAt = user.CreatedDate ?? user.CreatedAt
    };

    public static void ApplyDetails(ApplicationUser user, UserDetailsDto dto)
    {
        user.Email = dto.Email.Trim();
        user.UserName = dto.Email.Trim();
        user.DisplayName = dto.DisplayName.Trim();
        user.Active = dto.Active;
        user.IsActive = dto.Active;
        user.LastModifiedDate = DateTime.UtcNow;
    }
}

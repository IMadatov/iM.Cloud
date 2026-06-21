using iM.Cloud.Domain.Dtos.Groups;
using iM.Cloud.Domain.Entities;

namespace iM.Cloud.Domain.Mappings;

public static class GroupMappings
{
    public static GroupDetailsDto ToDetailsDto(Group group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Description = group.Description,
        CreatedAt = group.CreatedDate,
        Active = group.Active,
        CreatedBy = group.CreatedBy,
        LastModifiedBy = group.LastModifiedBy,
        LastModifiedAt = group.LastModifiedDate
    };

    public static void ApplyDetails(Group group, GroupDetailsDto dto)
    {
        group.Update(dto.Name, dto.Description);
        group.Active = dto.Active;
        group.LastModifiedBy = dto.LastModifiedBy;
        group.LastModifiedDate = DateTime.UtcNow;
    }
}

using BaseCrud.Abstractions.Entities;
using BaseCrud.Entities;
using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Entities;

namespace iM.Cloud.Domain.Dtos.Groups;

public sealed class GroupListDto : IDataTransferObject<Group, Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool Active { get; set; }
    public GroupAccessLevel? AccessLevel { get; set; }
}

public sealed class GroupMemberDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string? DisplayName { get; set; }
    public GroupAccessLevel AccessLevel { get; set; }
}

public sealed class GroupDetailsDto : IDataTransferObject<Group, Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool Active { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}

using BaseCrud.Entities;
using iM.Cloud.Infrastructure.Identity;

namespace iM.Cloud.Infrastructure.Dtos.Roles;

public sealed class RoleListDto : IDataTransferObject<ApplicationRole, Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool Active { get; set; }
}

public sealed class RoleDetailsDto : IDataTransferObject<ApplicationRole, Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool Active { get; set; }
}

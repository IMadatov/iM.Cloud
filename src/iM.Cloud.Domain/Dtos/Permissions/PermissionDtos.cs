using BaseCrud.Abstractions.Entities;
using BaseCrud.Entities;
using iM.Cloud.Domain.Entities;

namespace iM.Cloud.Domain.Dtos.Permissions;

public sealed class PermissionListDto : IDataTransferObject<Permission, Guid>
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool Active { get; set; }
}

public sealed class PermissionDetailsDto : IDataTransferObject<Permission, Guid>
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool Active { get; set; }
}

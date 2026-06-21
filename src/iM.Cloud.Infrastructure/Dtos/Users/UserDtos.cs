using BaseCrud.Entities;
using iM.Cloud.Infrastructure.Identity;

namespace iM.Cloud.Infrastructure.Dtos.Users;

public sealed class UserListDto : IDataTransferObject<ApplicationUser, Guid>
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool Active { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class UserDetailsDto : IDataTransferObject<ApplicationUser, Guid>
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool Active { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Password { get; set; }
}

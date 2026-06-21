using BaseCrud.Entities;
using Microsoft.AspNetCore.Identity;

namespace iM.Cloud.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>, IEntity<Guid>
{
    public string DisplayName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    public bool Active
    {
        get => IsActive;
        set => IsActive = value;
    }
}

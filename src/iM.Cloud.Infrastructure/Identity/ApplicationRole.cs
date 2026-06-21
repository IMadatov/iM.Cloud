using BaseCrud.Entities;
using Microsoft.AspNetCore.Identity;

namespace iM.Cloud.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>, IEntity<Guid>
{
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}

using BaseCrud.Abstractions.Services;
using BaseCrud.ServiceResults;
using iM.Cloud.Infrastructure.Dtos.Users;
using iM.Cloud.Infrastructure.Identity;

namespace iM.Cloud.Infrastructure.Admin.Users;

public interface IUserCrudService : ICrudService<ApplicationUser, UserListDto, UserDetailsDto, Guid, Guid>
{
    Task<ServiceResult> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<ServiceResult> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GrantPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
    Task<ServiceResult> RevokePermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default);
}

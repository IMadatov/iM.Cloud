using BaseCrud.Abstractions.Services;
using BaseCrud.ServiceResults;
using iM.Cloud.Infrastructure.Dtos.Roles;
using iM.Cloud.Infrastructure.Identity;

namespace iM.Cloud.Infrastructure.Admin.Roles;

public interface IRoleCrudService : ICrudService<ApplicationRole, RoleListDto, RoleDetailsDto, Guid, Guid>
{
    Task<ServiceResult> AssignPermissionAsync(Guid roleId, string permissionCode, CancellationToken cancellationToken = default);
    Task<ServiceResult> RemovePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
}

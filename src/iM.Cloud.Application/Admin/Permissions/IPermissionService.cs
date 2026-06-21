using BaseCrud.Abstractions.Services;
using iM.Cloud.Domain.Dtos.Permissions;
using iM.Cloud.Domain.Entities;

namespace iM.Cloud.Application.Admin.Permissions;

public interface IPermissionService : ICrudService<Permission, PermissionListDto, PermissionDetailsDto, Guid, Guid>;

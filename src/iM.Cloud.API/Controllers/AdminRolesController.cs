using BaseCrud.Abstractions.Entities;
using BaseCrud.PrimeNg;
using iM.Cloud.API.Authorization;
using iM.Cloud.API.Common;
using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Dtos.Permissions;
using iM.Cloud.Infrastructure.Admin.Roles;
using iM.Cloud.Infrastructure.Dtos.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Controllers;

[Authorize]
[Route("api/admin/roles")]
public sealed class AdminRolesController : ApiControllerBase
{
    private readonly IRoleCrudService _roleCrudService;

    public AdminRolesController(IRoleCrudService roleCrudService, ICurrentUserService currentUser)
        : base(currentUser)
        => _roleCrudService = roleCrudService;

    [HttpPost("GetAll")]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult<QueryResult<RoleListDto>?>> GetAll([FromBody] PrimeTableMetaData metaData)
        => FromServiceResult(_roleCrudService.GetAllAsync(metaData, UserProfile));

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult<RoleDetailsDto?>> GetById(Guid id, CancellationToken cancellationToken)
        => FromServiceResult(_roleCrudService.GetByIdAsync(id, UserProfile, cancellationToken: cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult<RoleDetailsDto?>> Create([FromBody] RoleDetailsDto request, CancellationToken cancellationToken)
        => FromServiceResult(_roleCrudService.InsertAsync(request, UserProfile, cancellationToken));

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult<RoleDetailsDto?>> Update(Guid id, [FromBody] RoleDetailsDto request, CancellationToken cancellationToken)
    {
        request.Id = id;
        return FromServiceResult(_roleCrudService.UpdateAsync(request, UserProfile, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        => FromServiceResult(_roleCrudService.DeactivateByIdAsync(id, UserProfile, cancellationToken: cancellationToken));

    [HttpGet("{roleId:guid}/permissions")]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult<List<PermissionListDto>?>> GetPermissions(Guid roleId, CancellationToken cancellationToken)
        => FromServiceResult(_roleCrudService.GetPermissionsAsync(roleId, cancellationToken));

    [HttpPost("{roleId:guid}/permissions")]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult> AssignPermission(Guid roleId, [FromBody] AssignPermissionRequest request, CancellationToken cancellationToken)
        => FromServiceResult(_roleCrudService.AssignPermissionAsync(roleId, request.PermissionCode, cancellationToken));

    [HttpDelete("{roleId:guid}/permissions/{permissionId:guid}")]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult> RemovePermission(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
        => FromServiceResult(_roleCrudService.RemovePermissionAsync(roleId, permissionId, cancellationToken));
}

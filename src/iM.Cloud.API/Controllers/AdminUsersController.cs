using BaseCrud.Abstractions.Entities;
using BaseCrud.PrimeNg;
using iM.Cloud.API.Authorization;
using iM.Cloud.API.Common;
using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Dtos.Permissions;
using iM.Cloud.Infrastructure.Dtos.Roles;
using iM.Cloud.Infrastructure.Admin.Users;
using iM.Cloud.Infrastructure.Dtos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Controllers;

[Authorize]
[Route("api/admin/users")]
public sealed class AdminUsersController : ApiControllerBase
{
    private readonly IUserCrudService _userCrudService;

    public AdminUsersController(IUserCrudService userCrudService, ICurrentUserService currentUser)
        : base(currentUser)
        => _userCrudService = userCrudService;

    [HttpPost("GetAll")]
    [RequirePermission(PermissionCodes.UsersRead)]
    public Task<ActionResult<QueryResult<UserListDto>?>> GetAll([FromBody] PrimeTableMetaData metaData)
        => FromServiceResult(_userCrudService.GetAllAsync(metaData, UserProfile));

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.UsersRead)]
    public Task<ActionResult<UserDetailsDto?>> GetById(Guid id, CancellationToken cancellationToken)
        => FromServiceResult(_userCrudService.GetByIdAsync(id, UserProfile, cancellationToken: cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.UsersCreate)]
    public Task<ActionResult<UserDetailsDto?>> Create([FromBody] UserDetailsDto request, CancellationToken cancellationToken)
        => FromServiceResult(_userCrudService.InsertAsync(request, UserProfile, cancellationToken));

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCodes.UsersUpdate)]
    public Task<ActionResult<UserDetailsDto?>> Update(Guid id, [FromBody] UserDetailsDto request, CancellationToken cancellationToken)
    {
        request.Id = id;
        return FromServiceResult(_userCrudService.UpdateAsync(request, UserProfile, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.UsersUpdate)]
    public Task<ActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        => FromServiceResult(_userCrudService.DeactivateByIdAsync(id, UserProfile, cancellationToken: cancellationToken));

    [HttpGet("{userId:guid}/roles")]
    [RequirePermission(PermissionCodes.UsersRead)]
    public Task<ActionResult<List<RoleListDto>?>> GetRoles(Guid userId, CancellationToken cancellationToken)
        => FromServiceResult(_userCrudService.GetRolesAsync(userId, cancellationToken));

    [HttpGet("{userId:guid}/permissions")]
    [RequirePermission(PermissionCodes.UsersRead)]
    public Task<ActionResult<List<PermissionListDto>?>> GetPermissions(Guid userId, CancellationToken cancellationToken)
        => FromServiceResult(_userCrudService.GetDirectPermissionsAsync(userId, cancellationToken));

    [HttpPost("{userId:guid}/roles")]
    [RequirePermission(PermissionCodes.UsersUpdate)]
    public Task<ActionResult> AssignRole(Guid userId, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
        => FromServiceResult(_userCrudService.AssignRoleAsync(userId, request.RoleName, cancellationToken));

    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [RequirePermission(PermissionCodes.UsersUpdate)]
    public Task<ActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken cancellationToken)
        => FromServiceResult(_userCrudService.RemoveRoleAsync(userId, roleId, cancellationToken));

    [HttpPost("{userId:guid}/permissions")]
    [RequirePermission(PermissionCodes.PermissionsAssign)]
    public Task<ActionResult> GrantPermission(Guid userId, [FromBody] AssignPermissionRequest request, CancellationToken cancellationToken)
        => FromServiceResult(_userCrudService.GrantPermissionAsync(userId, request.PermissionCode, cancellationToken));

    [HttpDelete("{userId:guid}/permissions/{permissionId:guid}")]
    [RequirePermission(PermissionCodes.PermissionsAssign)]
    public Task<ActionResult> RevokePermission(Guid userId, Guid permissionId, CancellationToken cancellationToken)
        => FromServiceResult(_userCrudService.RevokePermissionAsync(userId, permissionId, cancellationToken));
}

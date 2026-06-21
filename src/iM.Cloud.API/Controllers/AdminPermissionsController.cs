using BaseCrud.Abstractions.Entities;
using BaseCrud.PrimeNg;
using iM.Cloud.API.Authorization;
using iM.Cloud.API.Common;
using iM.Cloud.Application.Admin.Permissions;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Dtos.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Controllers;

[Authorize]
[Route("api/admin/permissions")]
public sealed class AdminPermissionsController : ApiControllerBase
{
    private readonly IPermissionService _permissionService;

    public AdminPermissionsController(IPermissionService permissionService, ICurrentUserService currentUser)
        : base(currentUser)
        => _permissionService = permissionService;

    [HttpPost("GetAll")]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult<QueryResult<PermissionListDto>?>> GetAll([FromBody] PrimeTableMetaData metaData)
        => FromServiceResult(_permissionService.GetAllAsync(metaData, UserProfile));

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.RolesManage)]
    public Task<ActionResult<PermissionDetailsDto?>> GetById(Guid id, CancellationToken cancellationToken)
        => FromServiceResult(_permissionService.GetByIdAsync(id, UserProfile, cancellationToken: cancellationToken));
}

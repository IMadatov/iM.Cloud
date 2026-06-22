using AutoMapper;
using BaseCrud.Abstractions.Entities;
using BaseCrud.EntityFrameworkCore;
using BaseCrud.Errors;
using BaseCrud.ServiceResults;
using iM.Cloud.Application.Common;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Domain.Dtos.Permissions;
using iM.Cloud.Domain.Entities;
using iM.Cloud.Infrastructure.Admin.Roles;
using iM.Cloud.Infrastructure.Dtos.Roles;
using iM.Cloud.Infrastructure.Identity;
using iM.Cloud.Infrastructure.Mappings;
using iM.Cloud.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace iM.Cloud.Infrastructure.Admin.Roles;

public sealed class RoleCrudService
    : BaseCrudService<ApplicationRole, RoleListDto, RoleDetailsDto, Guid, Guid>, IRoleCrudService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;
    private readonly IPermissionCache _permissionCache;

    public RoleCrudService(
        ApplicationDbContext dbContext,
        IMapper mapper,
        RoleManager<ApplicationRole> roleManager,
        IPermissionCache permissionCache)
        : base(dbContext, mapper)
    {
        _roleManager = roleManager;
        _db = dbContext;
        _permissionCache = permissionCache;
    }

    public override async Task<ServiceResult<RoleDetailsDto?>> GetByIdAsync(
        Guid id,
        IUserProfile<Guid>? userProfile,
        Func<CrudActionContext<ApplicationRole, Guid, Guid>, ValueTask<IQueryable<ApplicationRole>>>? customAction = null,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Db.NotFoundByIdMessage,
                ErrorKeys.Db.NotFoundById));

        return RoleMappings.ToDetailsDto(role);
    }

    public override async Task<ServiceResult<RoleDetailsDto>> InsertAsync(
        RoleDetailsDto entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
    {
        var name = entity.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.NameRequiredMessage,
                ErrorKeys.Validation.NameRequired));

        if (await _roleManager.RoleExistsAsync(name))
            return Conflict(new ValidationServiceError(
                ErrorKeys.Validation.RoleExistsMessage,
                ErrorKeys.Validation.RoleExists));

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = entity.Description?.Trim(),
            Active = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userProfile?.UserName
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.RoleCreateFailedMessage,
                ErrorKeys.Validation.RoleCreateFailed));
        }

        return RoleMappings.ToDetailsDto(role);
    }

    public override async Task<ServiceResult<RoleDetailsDto>> UpdateAsync(
        RoleDetailsDto entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(entity.Id.ToString());
        if (role is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Db.NotFoundByIdMessage,
                ErrorKeys.Db.NotFoundById));

        RoleMappings.ApplyDetails(role, entity);
        role.LastModifiedBy = userProfile?.UserName;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.RoleUpdateFailedMessage,
                ErrorKeys.Validation.RoleUpdateFailed));
        }

        await _permissionCache.InvalidateUsersInRoleAsync(role.Id, cancellationToken);
        return RoleMappings.ToDetailsDto(role);
    }

    public async Task<ServiceResult<List<PermissionListDto>>> GetPermissionsAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Roles.NotFoundMessage,
                ErrorKeys.Roles.NotFound));

        var permissions = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p)
            .OrderBy(p => p.Code)
            .Select(p => new PermissionListDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Active = p.Active
            })
            .ToListAsync(cancellationToken);

        return permissions;
    }

    public async Task<ServiceResult> AssignPermissionAsync(
        Guid roleId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Roles.NotFoundMessage,
                ErrorKeys.Roles.NotFound));

        var permission = await _db.Permissions
            .FirstOrDefaultAsync(p => p.Code == permissionCode, cancellationToken);

        if (permission is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Roles.PermissionNotFoundMessage,
                ErrorKeys.Roles.PermissionNotFound));

        var exists = await _db.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id, cancellationToken);

        if (!exists)
        {
            _db.RolePermissions.Add(RolePermission.Create(roleId, permission.Id));
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _permissionCache.InvalidateUsersInRoleAsync(roleId, cancellationToken);
        return NoContent();
    }

    public async Task<ServiceResult> RemovePermissionAsync(
        Guid roleId,
        Guid permissionId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

        if (entry is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Roles.PermissionLinkNotFoundMessage,
                ErrorKeys.Roles.PermissionLinkNotFound));

        _db.RolePermissions.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
        await _permissionCache.InvalidateUsersInRoleAsync(roleId, cancellationToken);
        return NoContent();
    }
}

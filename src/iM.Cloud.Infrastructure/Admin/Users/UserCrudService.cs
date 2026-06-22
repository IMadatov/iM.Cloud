using AutoMapper;
using BaseCrud.Abstractions.Entities;
using BaseCrud.EntityFrameworkCore;
using BaseCrud.Errors;
using BaseCrud.ServiceResults;
using iM.Cloud.Application.Common;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Domain.Dtos.Permissions;
using iM.Cloud.Domain.Entities;
using iM.Cloud.Infrastructure.Dtos.Roles;
using iM.Cloud.Infrastructure.Admin.Users;
using iM.Cloud.Infrastructure.Dtos.Users;
using iM.Cloud.Infrastructure.Identity;
using iM.Cloud.Infrastructure.Mappings;
using iM.Cloud.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace iM.Cloud.Infrastructure.Admin.Users;

public sealed class UserCrudService
    : BaseCrudService<ApplicationUser, UserListDto, UserDetailsDto, Guid, Guid>, IUserCrudService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;
    private readonly IPermissionCache _permissionCache;

    public UserCrudService(
        ApplicationDbContext dbContext,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IPermissionCache permissionCache)
        : base(dbContext, mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = dbContext;
        _permissionCache = permissionCache;
    }

    public override async Task<ServiceResult<UserDetailsDto?>> GetByIdAsync(
        Guid id,
        IUserProfile<Guid>? userProfile,
        Func<CrudActionContext<ApplicationUser, Guid, Guid>, ValueTask<IQueryable<ApplicationUser>>>? customAction = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Db.NotFoundByIdMessage,
                ErrorKeys.Db.NotFoundById));

        return UserMappings.ToDetailsDto(user);
    }

    public override async Task<ServiceResult<UserDetailsDto>> InsertAsync(
        UserDetailsDto entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
    {
        var email = entity.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.EmailRequiredMessage,
                ErrorKeys.Validation.EmailRequired));

        if (string.IsNullOrWhiteSpace(entity.Password))
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.PasswordRequiredMessage,
                ErrorKeys.Validation.PasswordRequired));

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return Conflict(new ValidationServiceError(
                ErrorKeys.Validation.EmailExistsMessage,
                ErrorKeys.Validation.EmailExists));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = entity.DisplayName.Trim(),
            IsActive = entity.Active,
            Active = entity.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userProfile?.UserName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, entity.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.UserCreateFailedMessage,
                ErrorKeys.Validation.UserCreateFailed));
        }

        return UserMappings.ToDetailsDto(user);
    }

    public override async Task<ServiceResult<UserDetailsDto>> UpdateAsync(
        UserDetailsDto entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(entity.Id.ToString());
        if (user is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Db.NotFoundByIdMessage,
                ErrorKeys.Db.NotFoundById));

        UserMappings.ApplyDetails(user, entity);
        user.LastModifiedBy = userProfile?.UserName;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.UserUpdateFailedMessage,
                ErrorKeys.Validation.UserUpdateFailed));
        }

        _permissionCache.InvalidateUser(user.Id);
        return UserMappings.ToDetailsDto(user);
    }

    public async Task<ServiceResult<List<RoleListDto>>> GetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Users.NotFoundMessage,
                ErrorKeys.Users.NotFound));

        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r)
            .OrderBy(r => r.Name)
            .Select(r => new RoleListDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Description = r.Description,
                Active = r.Active
            })
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task<ServiceResult<List<PermissionListDto>>> GetDirectPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Users.NotFoundMessage,
                ErrorKeys.Users.NotFound));

        var permissions = await _db.UserPermissions
            .Where(up => up.UserId == userId)
            .Join(_db.Permissions, up => up.PermissionId, p => p.Id, (_, p) => p)
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

    public async Task<ServiceResult> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Users.NotFoundMessage,
                ErrorKeys.Users.NotFound));

        if (!await _roleManager.RoleExistsAsync(roleName))
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Roles.NotFoundMessage,
                ErrorKeys.Roles.NotFound));

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                return BadRequest(new ValidationServiceError(
                    ErrorKeys.Validation.RoleAssignFailedMessage,
                    ErrorKeys.Validation.RoleAssignFailed));
            }
        }

        _permissionCache.InvalidateUser(userId);
        return NoContent();
    }

    public async Task<ServiceResult> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Users.NotFoundMessage,
                ErrorKeys.Users.NotFound));

        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role?.Name is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Roles.NotFoundMessage,
                ErrorKeys.Roles.NotFound));

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
        if (!result.Succeeded)
        {
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.RoleRemoveFailedMessage,
                ErrorKeys.Validation.RoleRemoveFailed));
        }

        _permissionCache.InvalidateUser(userId);
        return NoContent();
    }

    public async Task<ServiceResult> GrantPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var permission = await _db.Permissions
            .FirstOrDefaultAsync(p => p.Code == permissionCode, cancellationToken);

        if (permission is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Permissions.NotFoundMessage,
                ErrorKeys.Permissions.NotFound));

        var exists = await _db.UserPermissions
            .AnyAsync(up => up.UserId == userId && up.PermissionId == permission.Id, cancellationToken);

        if (!exists)
        {
            _db.UserPermissions.Add(UserPermission.Create(userId, permission.Id));
            await _db.SaveChangesAsync(cancellationToken);
        }

        _permissionCache.InvalidateUser(userId);
        return NoContent();
    }

    public async Task<ServiceResult> RevokePermissionAsync(
        Guid userId,
        Guid permissionId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId, cancellationToken);

        if (entry is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Permissions.UserLinkNotFoundMessage,
                ErrorKeys.Permissions.UserLinkNotFound));

        _db.UserPermissions.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
        _permissionCache.InvalidateUser(userId);
        return NoContent();
    }
}

using AutoMapper;
using BaseCrud.Abstractions.Entities;
using BaseCrud.EntityFrameworkCore;
using BaseCrud.Errors;
using BaseCrud.ServiceResults;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Domain.Entities;
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
            return NotFound(new NotFoundServiceError());

        return UserMappings.ToDetailsDto(user);
    }

    public override async Task<ServiceResult<UserDetailsDto>> InsertAsync(
        UserDetailsDto entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
    {
        var email = entity.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new ValidationServiceError("Email is required.", "validation.email_required"));

        if (string.IsNullOrWhiteSpace(entity.Password))
            return BadRequest(new ValidationServiceError("Password is required.", "validation.password_required"));

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return Conflict(new ValidationServiceError("Email already exists.", "validation.email_exists"));

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
                string.Join("; ", result.Errors.Select(e => e.Description)),
                "validation.user_create_failed"));
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
            return NotFound(new NotFoundServiceError());

        UserMappings.ApplyDetails(user, entity);
        user.LastModifiedBy = userProfile?.UserName;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new ValidationServiceError(
                string.Join("; ", result.Errors.Select(e => e.Description)),
                "validation.user_update_failed"));
        }

        _permissionCache.InvalidateUser(user.Id);
        return UserMappings.ToDetailsDto(user);
    }

    public async Task<ServiceResult> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound(new NotFoundServiceError("User not found."));

        if (!await _roleManager.RoleExistsAsync(roleName))
            return NotFound(new NotFoundServiceError("Role not found."));

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                return BadRequest(new ValidationServiceError(
                    string.Join("; ", result.Errors.Select(e => e.Description)),
                    "validation.role_assign_failed"));
            }
        }

        _permissionCache.InvalidateUser(userId);
        return NoContent();
    }

    public async Task<ServiceResult> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound(new NotFoundServiceError("User not found."));

        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role?.Name is null)
            return NotFound(new NotFoundServiceError("Role not found."));

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
        if (!result.Succeeded)
        {
            return BadRequest(new ValidationServiceError(
                string.Join("; ", result.Errors.Select(e => e.Description)),
                "validation.role_remove_failed"));
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
            return NotFound(new NotFoundServiceError("Permission not found."));

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
            return NotFound(new NotFoundServiceError("User permission not found."));

        _db.UserPermissions.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
        _permissionCache.InvalidateUser(userId);
        return NoContent();
    }
}

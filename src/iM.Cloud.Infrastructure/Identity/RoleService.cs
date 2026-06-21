using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Common.Models;
using iM.Cloud.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace iM.Cloud.Infrastructure.Identity;

public sealed class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public RoleService(RoleManager<ApplicationRole> roleManager, ApplicationDbContext db)
    {
        _roleManager = roleManager;
        _db = db;
    }

    public async Task<Result<RoleDto>> CreateAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        if (await _roleManager.RoleExistsAsync(name))
            return Result<RoleDto>.Failure("Role already exists.");

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            NormalizedName = name.Trim().ToUpperInvariant(),
            Description = description?.Trim()
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return Result<RoleDto>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Result<RoleDto>.Success(await MapAsync(role));
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var list = new List<RoleDto>();
        foreach (var role in roles)
            list.Add(await MapAsync(role));

        return list;
    }

    public async Task<RoleDto?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        return role is null ? null : await MapAsync(role);
    }

    public async Task<RoleDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(name);
        return role is null ? null : await MapAsync(role);
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role?.Name is null)
            return [];

        var users = await _db.Users
            .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return users;
    }

    private async Task<RoleDto> MapAsync(ApplicationRole role)
    {
        var permissionCodes = await _db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p.Code)
            .OrderBy(c => c)
            .ToListAsync();

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            Permissions = permissionCodes
        };
    }
}

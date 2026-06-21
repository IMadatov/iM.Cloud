using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Common.Models;
using iM.Cloud.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace iM.Cloud.Infrastructure.Identity;

public sealed class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Result<UserDto>.Failure("Email already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Result<UserDto>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Result<UserDto>.Success(await MapAsync(user));
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync(cancellationToken);
        var list = new List<UserDto>();
        foreach (var user in users)
            list.Add(await MapAsync(user));

        return list;
    }

    public async Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await MapAsync(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : await MapAsync(user);
    }

    public async Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure("User not found.");

        if (!await _roleManager.RoleExistsAsync(roleName))
            return Result.Failure("Role not found.");

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
                return Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure("User not found.");

        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.Name is null)
            return Result.Failure("Role not found.");

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public async Task<Result> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return Result.Failure("Invalid credentials.");

        if (!await _userManager.CheckPasswordAsync(user, password))
            return Result.Failure("Invalid credentials.");

        return Result.Success();
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return [];

        var roles = await _userManager.GetRolesAsync(user);
        return roles.OrderBy(r => r).ToList();
    }

    private async Task<UserDto> MapAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = roles.OrderBy(r => r).ToList()
        };
    }
}

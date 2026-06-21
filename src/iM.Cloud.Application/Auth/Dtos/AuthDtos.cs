namespace iM.Cloud.Application.Auth.Dtos;

public sealed class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = null!;
}

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime AccessTokenExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public sealed class CreateUserRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
}

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}

public sealed class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public IReadOnlyList<string> Permissions { get; set; } = [];
}

public sealed class PermissionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public sealed class GroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateGroupRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public sealed class UpdateGroupRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public sealed class AssignRoleRequest
{
    public string RoleName { get; set; } = null!;
}

public sealed class CreateRoleRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public sealed class AddGroupMemberRequest
{
    public Guid UserId { get; set; }
}

public sealed class AssignPermissionRequest
{
    public string PermissionCode { get; set; } = null!;
}

public sealed class MeResponse
{
    public UserDto User { get; set; } = null!;
    public IReadOnlyList<string> Permissions { get; set; } = [];
}

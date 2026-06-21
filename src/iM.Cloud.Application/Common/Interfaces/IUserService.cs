using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Models;

namespace iM.Cloud.Application.Common.Interfaces;

public interface IUserService
{
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<Result> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<Result> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);
}

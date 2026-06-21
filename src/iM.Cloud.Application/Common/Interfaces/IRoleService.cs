using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Models;

namespace iM.Cloud.Application.Common.Interfaces;

public interface IRoleService
{
    Task<Result<RoleDto>> CreateAsync(string name, string? description, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<RoleDto?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<RoleDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}

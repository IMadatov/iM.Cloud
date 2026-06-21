using iM.Cloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace iM.Cloud.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermission> UserPermissions { get; }
    DbSet<Group> Groups { get; }
    DbSet<UserGroup> UserGroups { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

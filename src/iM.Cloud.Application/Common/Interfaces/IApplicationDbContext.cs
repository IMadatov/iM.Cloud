using iM.Cloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FileShareEntity = iM.Cloud.Domain.Entities.FileShare;

namespace iM.Cloud.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermission> UserPermissions { get; }
    DbSet<Group> Groups { get; }
    DbSet<UserGroup> UserGroups { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<StorageObject> StorageObjects { get; }
    DbSet<FileItem> FileItems { get; }
    DbSet<FileShareEntity> FileShares { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

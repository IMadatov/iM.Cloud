using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iM.Cloud.Infrastructure.Authorization;

public sealed class PermissionResolver : IPermissionResolver
{
    private readonly ApplicationDbContext _db;

    public PermissionResolver(ApplicationDbContext db) => _db = db;

    public async Task<HashSet<string>> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var rolePermissions = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p.Code)
            .ToListAsync(cancellationToken);

        var userPermissions = await _db.UserPermissions
            .Where(up => up.UserId == userId)
            .Join(_db.Permissions, up => up.PermissionId, p => p.Id, (_, p) => p.Code)
            .ToListAsync(cancellationToken);

        return rolePermissions.Concat(userPermissions).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

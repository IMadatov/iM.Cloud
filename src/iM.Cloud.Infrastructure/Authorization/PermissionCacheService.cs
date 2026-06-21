using iM.Cloud.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace iM.Cloud.Infrastructure.Authorization;

public sealed class PermissionCacheService : IPermissionCache
{
    private const int CacheMinutes = 30;
    private readonly IMemoryCache _cache;
    private readonly IPermissionResolver _resolver;
    private readonly IRoleService _roleService;

    public PermissionCacheService(
        IMemoryCache cache,
        IPermissionResolver resolver,
        IRoleService roleService)
    {
        _cache = cache;
        _resolver = resolver;
        _roleService = roleService;
    }

    public async Task<HashSet<string>> GetOrLoadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(userId);
        if (_cache.TryGetValue(key, out HashSet<string>? cached) && cached is not null)
            return cached;

        var permissions = await _resolver.ResolveAsync(userId, cancellationToken);
        _cache.Set(key, permissions, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(CacheMinutes)
        });

        return permissions;
    }

    public void InvalidateUser(Guid userId) => _cache.Remove(CacheKey(userId));

    public async Task InvalidateUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var userIds = await _roleService.GetUserIdsInRoleAsync(roleId, cancellationToken);
        foreach (var userId in userIds)
            InvalidateUser(userId);
    }

    private static string CacheKey(Guid userId) => $"permissions:user:{userId}";
}

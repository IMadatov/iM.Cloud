using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Common.Models;

namespace iM.Cloud.Application.Navigation;

public sealed class GetMyNavigationHandler
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionCache _permissionCache;

    public GetMyNavigationHandler(
        ICurrentUserService currentUser,
        IPermissionCache permissionCache)
    {
        _currentUser = currentUser;
        _permissionCache = permissionCache;
    }

    public async Task<Result<IReadOnlyList<NavigationItemDto>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
            return Result<IReadOnlyList<NavigationItemDto>>.Failure("Unauthorized.");

        var permissions = await _permissionCache.GetOrLoadAsync(userId, cancellationToken);

        var items = NavigationCatalog.Items
            .Where(entry => entry.RequiredPermission is null
                || permissions.Contains(entry.RequiredPermission))
            .OrderBy(entry => entry.Order)
            .Select(entry => new NavigationItemDto
            {
                Key = entry.Key,
                Label = entry.Label,
                Icon = entry.Icon,
                Path = entry.Path,
                Order = entry.Order
            })
            .ToList();

        return Result<IReadOnlyList<NavigationItemDto>>.Success(items);
    }
}

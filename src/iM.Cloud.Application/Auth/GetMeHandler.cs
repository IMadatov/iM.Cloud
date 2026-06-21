using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Common.Models;

namespace iM.Cloud.Application.Auth;

public sealed class GetMeHandler
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserService _userService;
    private readonly IPermissionCache _permissionCache;

    public GetMeHandler(
        ICurrentUserService currentUser,
        IUserService userService,
        IPermissionCache permissionCache)
    {
        _currentUser = currentUser;
        _userService = userService;
        _permissionCache = permissionCache;
    }

    public async Task<Result<MeResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
            return Result<MeResponse>.Failure("Unauthorized.");

        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<MeResponse>.Failure("User not found.");

        var permissions = await _permissionCache.GetOrLoadAsync(userId, cancellationToken);

        return Result<MeResponse>.Success(new MeResponse
        {
            User = user,
            Permissions = permissions.OrderBy(p => p).ToList()
        });
    }
}

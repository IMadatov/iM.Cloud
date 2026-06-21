using iM.Cloud.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace iM.Cloud.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = PermissionPolicy.Name(permission);
    }
}

public static class PermissionPolicy
{
    public static string Name(string permission) => $"Permission:{permission}";
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionCache _permissionCache;
    private readonly ICurrentUserService _currentUser;

    public PermissionAuthorizationHandler(IPermissionCache permissionCache, ICurrentUserService currentUser)
    {
        _permissionCache = permissionCache;
        _currentUser = currentUser;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (_currentUser.UserId is not Guid userId)
            return;

        var permissions = await _permissionCache.GetOrLoadAsync(userId);
        if (permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;
    public string Permission { get; }
}

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            foreach (var permission in Domain.Authorization.PermissionCodes.All)
            {
                options.AddPolicy(PermissionPolicy.Name(permission), policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });

        return services;
    }
}

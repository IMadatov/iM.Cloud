using BaseCrud.Abstractions.Entities;
using BaseCrud.Errors;
using BaseCrud.ServiceResults;
using iM.Cloud.Application.Common;
using iM.Cloud.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Common;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private readonly ICurrentUserService _currentUser;

    protected ApiControllerBase(ICurrentUserService currentUser)
        => _currentUser = currentUser;

    protected IUserProfile<Guid>? UserProfile
    {
        get
        {
            if (_currentUser.UserId is not Guid userId)
                return null;

            return new ImCloudUserProfile
            {
                Id = userId,
                UserName = User.Identity?.Name,
                Fullname = User.FindFirst("display_name")?.Value ?? User.Identity?.Name
            };
        }
    }

    protected async Task<ActionResult<T?>> FromServiceResult<T>(Task<ServiceResult<T>> serviceAction)
    {
        try
        {
            var actionResult = await serviceAction;

            if (actionResult.TryGetResult(out var result))
                return result is not null ? result : NoContent();

            return StatusCode(actionResult.StatusCode, actionResult.Errors);
        }
        catch (Exception)
        {
            return StatusCode(500, new ServiceError(
                ErrorKeys.Server.UnhandledMessage,
                ErrorKeys.Server.Unhandled,
                null));
        }
    }

    protected async Task<ActionResult> FromServiceResult(Task<ServiceResult> serviceAction)
    {
        try
        {
            var actionResult = await serviceAction;

            if (actionResult.IsSuccess)
                return StatusCode(actionResult.StatusCode);

            return StatusCode(actionResult.StatusCode, actionResult.Errors);
        }
        catch (Exception)
        {
            return StatusCode(500, new ServiceError(
                ErrorKeys.Server.UnhandledMessage,
                ErrorKeys.Server.Unhandled,
                null));
        }
    }
}

using iM.Cloud.Application.Common.Models;
using iM.Cloud.Application.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Controllers;

[ApiController]
[Authorize]
[Route("api/navigation")]
public sealed class NavigationController : ControllerBase
{
    private readonly GetMyNavigationHandler _getMyNavigationHandler;

    public NavigationController(GetMyNavigationHandler getMyNavigationHandler)
        => _getMyNavigationHandler = getMyNavigationHandler;

    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<NavigationItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NavigationItemDto>>> My(CancellationToken cancellationToken)
    {
        var result = await _getMyNavigationHandler.HandleAsync(cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(Result<T> result) =>
        result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
}

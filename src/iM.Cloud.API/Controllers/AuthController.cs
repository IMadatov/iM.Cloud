using iM.Cloud.Application.Auth;
using iM.Cloud.Application.Auth.Dtos;
using iM.Cloud.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;
    private readonly RefreshTokenHandler _refreshTokenHandler;
    private readonly GetMeHandler _getMeHandler;

    public AuthController(
        LoginHandler loginHandler,
        RefreshTokenHandler refreshTokenHandler,
        GetMeHandler getMeHandler)
    {
        _loginHandler = loginHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _getMeHandler = getMeHandler;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _refreshTokenHandler.HandleAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _getMeHandler.HandleAsync(cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result) =>
        result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });

    private IActionResult ToActionResult(Result result) =>
        result.Succeeded ? Ok() : BadRequest(new { error = result.Error });
}

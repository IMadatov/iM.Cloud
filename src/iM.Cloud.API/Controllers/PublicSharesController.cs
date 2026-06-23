using iM.Cloud.API.Common;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Controllers;

[AllowAnonymous]
[Route("api/public/shares")]
public sealed class PublicSharesController : ApiControllerBase
{
    private readonly IFileService _fileService;

    public PublicSharesController(IFileService fileService, ICurrentUserService currentUser)
        : base(currentUser)
        => _fileService = fileService;

    [HttpGet("{token}")]
    [ProducesResponseType(typeof(SharePreviewDto), StatusCodes.Status200OK)]
    public Task<ActionResult<SharePreviewDto?>> Get(string token, CancellationToken cancellationToken)
        => FromServiceResult(_fileService.GetSharePreviewAsync(token, cancellationToken));

    [HttpGet("{token}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContent(
        string token,
        [FromQuery] string disposition = "inline",
        CancellationToken cancellationToken = default)
    {
        var result = await _fileService.GetShareDownloadAsync(token, cancellationToken);
        if (result.TryGetResult(out var download) && download is not null)
        {
            if (string.Equals(disposition, "attachment", StringComparison.OrdinalIgnoreCase))
                return File(download.Content, download.ContentType, download.FileName);

            return File(download.Content, download.ContentType);
        }

        return StatusCode(result.StatusCode, result.Errors);
    }
}

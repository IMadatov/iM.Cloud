using iM.Cloud.API.Common;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Controllers;

[Authorize]
[Route("api/files")]
public sealed class FilesController : ApiControllerBase
{
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUser;

    public FilesController(IFileService fileService, ICurrentUserService currentUser)
        : base(currentUser)
    {
        _fileService = fileService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FileItemDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<IReadOnlyList<FileItemDto>?>> List([FromQuery] Guid? parentId, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<IReadOnlyList<FileItemDto>?>>(Unauthorized());

        return FromServiceResult(_fileService.ListAsync(parentId, userId, cancellationToken));
    }

    [HttpPost("folders")]
    [ProducesResponseType(typeof(FileItemDto), StatusCodes.Status200OK)]
    public Task<ActionResult<FileItemDto?>> CreateFolder([FromBody] CreateFolderRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<FileItemDto?>>(Unauthorized());

        return FromServiceResult(_fileService.CreateFolderAsync(request, userId, User.Identity?.Name, cancellationToken));
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileItemDto), StatusCodes.Status200OK)]
    [RequestSizeLimit(524_288_000)]
    public async Task<ActionResult<FileItemDto?>> Upload(
        [FromForm] Guid? parentId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Unauthorized();

        if (file.Length == 0)
            return BadRequest();

        await using var stream = file.OpenReadStream();
        var result = await _fileService.UploadAsync(
            parentId,
            file.FileName,
            stream,
            file.Length,
            file.ContentType,
            userId,
            User.Identity?.Name,
            cancellationToken);

        if (result.TryGetResult(out var dto))
            return dto is not null ? dto : NoContent();

        return StatusCode(result.StatusCode, result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult>(Unauthorized());

        return FromServiceResult(_fileService.DeleteAsync(id, userId, cancellationToken));
    }

    [HttpPut("{id:guid}/rename")]
    [ProducesResponseType(typeof(FileItemDto), StatusCodes.Status200OK)]
    public Task<ActionResult<FileItemDto?>> Rename(
        Guid id,
        [FromBody] RenameFileRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<FileItemDto?>>(Unauthorized());

        return FromServiceResult(_fileService.RenameAsync(id, request, userId, User.Identity?.Name, cancellationToken));
    }

    [HttpPut("{id:guid}/move")]
    [ProducesResponseType(typeof(FileItemDto), StatusCodes.Status200OK)]
    public Task<ActionResult<FileItemDto?>> Move(
        Guid id,
        [FromBody] MoveFileRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<FileItemDto?>>(Unauthorized());

        return FromServiceResult(_fileService.MoveAsync(id, request, userId, User.Identity?.Name, cancellationToken));
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Unauthorized();

        var result = await _fileService.GetDownloadAsync(id, userId, cancellationToken);
        if (result.TryGetResult(out var download) && download is not null)
            return File(download.Content, download.ContentType, download.FileName);

        return StatusCode(result.StatusCode, result.Errors);
    }

    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status200OK)]
    public Task<ActionResult<ShareLinkDto?>> Share(
        Guid id,
        [FromBody] CreateShareRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<ShareLinkDto?>>(Unauthorized());

        return FromServiceResult(_fileService.CreateShareAsync(id, userId, request, cancellationToken));
    }

    [HttpDelete("shares/{token}")]
    public Task<ActionResult> RevokeShare(string token, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult>(Unauthorized());

        return FromServiceResult(_fileService.RevokeShareAsync(token, userId, cancellationToken));
    }
}

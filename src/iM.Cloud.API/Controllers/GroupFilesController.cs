using iM.Cloud.API.Common;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Controllers;

[Authorize]
[Route("api/groups/{groupId:guid}/files")]
public sealed class GroupFilesController : ApiControllerBase
{
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUser;

    public GroupFilesController(IFileService fileService, ICurrentUserService currentUser)
        : base(currentUser)
    {
        _fileService = fileService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FileItemDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<IReadOnlyList<FileItemDto>?>> List(
        Guid groupId,
        [FromQuery] Guid? parentId,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<IReadOnlyList<FileItemDto>?>>(Unauthorized());

        return FromServiceResult(_fileService.ListGroupAsync(groupId, parentId, userId, cancellationToken));
    }

    [HttpPost("folders")]
    [ProducesResponseType(typeof(FileItemDto), StatusCodes.Status200OK)]
    public Task<ActionResult<FileItemDto?>> CreateFolder(
        Guid groupId,
        [FromBody] CreateFolderRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<FileItemDto?>>(Unauthorized());

        return FromServiceResult(_fileService.CreateGroupFolderAsync(
            groupId,
            request,
            userId,
            User.Identity?.Name,
            cancellationToken));
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileItemDto), StatusCodes.Status200OK)]
    [RequestSizeLimit(524_288_000)]
    public async Task<ActionResult<FileItemDto?>> Upload(
        Guid groupId,
        [FromForm] Guid? parentId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Unauthorized();

        if (file.Length == 0)
            return BadRequest();

        await using var stream = file.OpenReadStream();
        var result = await _fileService.UploadGroupAsync(
            groupId,
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
    public Task<ActionResult> Delete(Guid groupId, Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult>(Unauthorized());

        return FromServiceResult(_fileService.DeleteGroupAsync(groupId, id, userId, cancellationToken));
    }

    [HttpPut("{id:guid}/rename")]
    [ProducesResponseType(typeof(FileItemDto), StatusCodes.Status200OK)]
    public Task<ActionResult<FileItemDto?>> Rename(
        Guid groupId,
        Guid id,
        [FromBody] RenameFileRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<FileItemDto?>>(Unauthorized());

        return FromServiceResult(_fileService.RenameGroupAsync(
            groupId,
            id,
            request,
            userId,
            User.Identity?.Name,
            cancellationToken));
    }

    [HttpPut("{id:guid}/move")]
    [ProducesResponseType(typeof(FileItemDto), StatusCodes.Status200OK)]
    public Task<ActionResult<FileItemDto?>> Move(
        Guid groupId,
        Guid id,
        [FromBody] MoveFileRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<FileItemDto?>>(Unauthorized());

        return FromServiceResult(_fileService.MoveGroupAsync(
            groupId,
            id,
            request,
            userId,
            User.Identity?.Name,
            cancellationToken));
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(Guid groupId, Guid id, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Unauthorized();

        var result = await _fileService.GetGroupDownloadAsync(groupId, id, userId, cancellationToken);
        if (result.TryGetResult(out var download) && download is not null)
            return File(download.Content, download.ContentType, download.FileName);

        return StatusCode(result.StatusCode, result.Errors);
    }

    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status200OK)]
    public Task<ActionResult<ShareLinkDto?>> Share(
        Guid groupId,
        Guid id,
        [FromBody] CreateShareRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<ShareLinkDto?>>(Unauthorized());

        return FromServiceResult(_fileService.CreateGroupShareAsync(
            groupId,
            id,
            userId,
            request,
            cancellationToken));
    }
}

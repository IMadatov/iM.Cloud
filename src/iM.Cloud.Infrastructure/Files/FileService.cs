using BaseCrud.Errors;
using BaseCrud.ServiceResults;
using iM.Cloud.Application.Common;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Files;
using iM.Cloud.Domain.Entities;
using iM.Cloud.Infrastructure.Common;
using iM.Cloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace iM.Cloud.Infrastructure.Files;

public sealed class FileService : IFileService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly ILogger<FileService> _logger;

    public FileService(ApplicationDbContext db, IFileStorageService storage, ILogger<FileService> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<ServiceResult<IReadOnlyList<FileItemDto>>> ListAsync(
        Guid? parentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (parentId is Guid parentFolderId)
        {
            var parentCheck = await ValidateParentFolderAsync(parentFolderId, userId, cancellationToken);
            if (!parentCheck.IsSuccess)
                return (ServiceResult<IReadOnlyList<FileItemDto>>)parentCheck;
        }

        var items = await _db.FileItems
            .AsNoTracking()
            .Where(f => f.OwnerId == userId && f.Active && f.ParentId == parentId)
            .OrderByDescending(f => f.IsFolder)
            .ThenBy(f => f.Name)
            .Select(f => new FileItemDto
            {
                Id = f.Id,
                Name = f.Name,
                IsFolder = f.IsFolder,
                ParentId = f.ParentId,
                Size = f.IsFolder ? null : f.StorageObject!.Size,
                ContentType = f.IsFolder ? null : f.StorageObject!.ContentType
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<ServiceResult<FileItemDto>> CreateFolderAsync(
        CreateFolderRequest request,
        Guid userId,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResultHelpers.BadRequest<FileItemDto>(
                new ValidationServiceError(ErrorKeys.Files.NameRequiredMessage, ErrorKeys.Files.NameRequired));

        try
        {
            FileItem.ValidateName(name);
        }
        catch (ArgumentException)
        {
            return ServiceResultHelpers.BadRequest<FileItemDto>(
                new ValidationServiceError(ErrorKeys.Files.InvalidNameMessage, ErrorKeys.Files.InvalidName));
        }

        if (request.ParentId is Guid parentId)
        {
            var parentCheck = await ValidateParentFolderAsync(parentId, userId, cancellationToken);
            if (!parentCheck.IsSuccess)
                return (ServiceResult<FileItemDto>)parentCheck;
        }

        if (await NameExistsAsync(userId, request.ParentId, name, cancellationToken))
            return ServiceResultHelpers.Conflict<FileItemDto>(
                new ValidationServiceError(ErrorKeys.Files.NameExistsMessage, ErrorKeys.Files.NameExists));

        var folder = FileItem.CreateFolder(name, request.ParentId, userId, userName);
        _db.FileItems.Add(folder);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(folder, null);
    }

    public async Task<ServiceResult<FileItemDto>> UploadAsync(
        Guid? parentId,
        string fileName,
        Stream content,
        long size,
        string? contentType,
        Guid userId,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        var name = fileName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResultHelpers.BadRequest<FileItemDto>(
                new ValidationServiceError(ErrorKeys.Files.NameRequiredMessage, ErrorKeys.Files.NameRequired));

        try
        {
            FileItem.ValidateName(name);
        }
        catch (ArgumentException)
        {
            return ServiceResultHelpers.BadRequest<FileItemDto>(
                new ValidationServiceError(ErrorKeys.Files.InvalidNameMessage, ErrorKeys.Files.InvalidName));
        }

        if (size < 0)
            return ServiceResultHelpers.BadRequest<FileItemDto>(
                new ValidationServiceError(ErrorKeys.Files.InvalidNameMessage, ErrorKeys.Files.InvalidName));

        if (parentId is Guid parentFolderId)
        {
            var parentCheck = await ValidateParentFolderAsync(parentFolderId, userId, cancellationToken);
            if (!parentCheck.IsSuccess)
                return (ServiceResult<FileItemDto>)parentCheck;
        }

        if (await NameExistsAsync(userId, parentId, name, cancellationToken))
            return ServiceResultHelpers.Conflict<FileItemDto>(
                new ValidationServiceError(ErrorKeys.Files.NameExistsMessage, ErrorKeys.Files.NameExists));

        var storageObject = StorageObject.Create(size, contentType, createdBy: userName);
        var uploaded = false;

        try
        {
            await _storage.UploadAsync(storageObject.StorageKey, content, size, contentType, cancellationToken);
            uploaded = true;

            _db.StorageObjects.Add(storageObject);
            var fileItem = FileItem.CreateFile(name, parentId, userId, storageObject.Id, userName);
            _db.FileItems.Add(fileItem);
            await _db.SaveChangesAsync(cancellationToken);

            return ToDto(fileItem, storageObject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} for user {UserId}", name, userId);

            if (uploaded)
            {
                try
                {
                    await _storage.DeleteAsync(storageObject.StorageKey, cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback blob {StorageKey}", storageObject.StorageKey);
                }
            }

            return ServiceResultHelpers.InternalServerError<FileItemDto>(
                new ServiceError(ErrorKeys.Files.StorageFailedMessage, ErrorKeys.Files.StorageFailed, null));
        }
    }

    public async Task<ServiceResult> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.FileItems
            .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == userId && f.Active, cancellationToken);

        if (item is null)
            return ServiceResult.NotFound(new NotFoundServiceError(
                ErrorKeys.Files.NotFoundMessage,
                ErrorKeys.Files.NotFound));

        var idsToDeactivate = new List<Guid> { item.Id };

        if (item.IsFolder)
        {
            var descendantIds = await CollectDescendantIdsAsync(item.Id, userId, cancellationToken);
            idsToDeactivate.AddRange(descendantIds);
        }

        var items = await _db.FileItems
            .Where(f => idsToDeactivate.Contains(f.Id) && f.OwnerId == userId && f.Active)
            .ToListAsync(cancellationToken);

        foreach (var fileItem in items)
            fileItem.Active = false;

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.NoContent();
    }

    public async Task<ServiceResult<FileDownloadResult>> GetDownloadAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var fileItem = await _db.FileItems
            .Include(f => f.StorageObject)
            .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == userId && f.Active, cancellationToken);

        if (fileItem is null)
            return ServiceResultHelpers.NotFound<FileDownloadResult>(
                new NotFoundServiceError(ErrorKeys.Files.NotFoundMessage, ErrorKeys.Files.NotFound));

        if (fileItem.IsFolder || fileItem.StorageObject is null)
            return ServiceResultHelpers.BadRequest<FileDownloadResult>(
                new ValidationServiceError(ErrorKeys.Files.NotAFileMessage, ErrorKeys.Files.NotAFile));

        try
        {
            var stream = await _storage.OpenReadAsync(fileItem.StorageObject.StorageKey, cancellationToken);
            return new FileDownloadResult
            {
                Content = stream,
                ContentType = fileItem.StorageObject.ContentType ?? "application/octet-stream",
                FileName = fileItem.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file {FileId} for user {UserId}", id, userId);
            return ServiceResultHelpers.InternalServerError<FileDownloadResult>(
                new ServiceError(ErrorKeys.Files.StorageFailedMessage, ErrorKeys.Files.StorageFailed, null));
        }
    }

    private async Task<IReadOnlyList<Guid>> CollectDescendantIdsAsync(
        Guid folderId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var allItems = await _db.FileItems
            .AsNoTracking()
            .Where(f => f.OwnerId == userId && f.Active)
            .Select(f => new { f.Id, f.ParentId })
            .ToListAsync(cancellationToken);

        var childrenByParent = allItems
            .Where(f => f.ParentId.HasValue)
            .GroupBy(f => f.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var result = new List<Guid>();
        var queue = new Queue<Guid>();

        if (childrenByParent.TryGetValue(folderId, out var directChildren))
        {
            foreach (var childId in directChildren)
                queue.Enqueue(childId);
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            result.Add(currentId);

            if (childrenByParent.TryGetValue(currentId, out var children))
            {
                foreach (var childId in children)
                    queue.Enqueue(childId);
            }
        }

        return result;
    }

    private async Task<ServiceResult> ValidateParentFolderAsync(
        Guid parentId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var parent = await _db.FileItems
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == parentId && f.OwnerId == userId && f.Active, cancellationToken);

        if (parent is null)
            return ServiceResult.NotFound(new NotFoundServiceError(
                ErrorKeys.Files.NotFoundMessage,
                ErrorKeys.Files.NotFound));

        if (!parent.IsFolder)
            return ServiceResult.BadRequest(new ValidationServiceError(
                ErrorKeys.Files.ParentNotFolderMessage,
                ErrorKeys.Files.ParentNotFolder));

        return ServiceResult.NoContent();
    }

    private Task<bool> NameExistsAsync(
        Guid userId,
        Guid? parentId,
        string name,
        CancellationToken cancellationToken) =>
        _db.FileItems.AnyAsync(
            f => f.OwnerId == userId && f.Active && f.ParentId == parentId && f.Name == name,
            cancellationToken);

    private static FileItemDto ToDto(FileItem item, StorageObject? storageObject) => new()
    {
        Id = item.Id,
        Name = item.Name,
        IsFolder = item.IsFolder,
        ParentId = item.ParentId,
        Size = item.IsFolder ? null : storageObject?.Size,
        ContentType = item.IsFolder ? null : storageObject?.ContentType
    };
}

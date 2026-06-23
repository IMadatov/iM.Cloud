using BaseCrud.Errors;
using BaseCrud.ServiceResults;
using iM.Cloud.Application.Common;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Application.Files;
using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Entities;
using FileShareEntity = iM.Cloud.Domain.Entities.FileShare;
using iM.Cloud.Infrastructure.Common;
using iM.Cloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

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
            .Where(f => f.OwnerId == userId && f.GroupId == null && f.Active && f.ParentId == parentId)
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

        var folder = FileItem.CreateFolder(name, request.ParentId, userId, createdBy: userName);
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
            var fileItem = FileItem.CreateFile(name, parentId, userId, storageObject.Id, createdBy: userName);
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
            .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == userId && f.GroupId == null && f.Active, cancellationToken);

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
            .Where(f => idsToDeactivate.Contains(f.Id) && f.OwnerId == userId && f.GroupId == null && f.Active)
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
            .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == userId && f.GroupId == null && f.Active, cancellationToken);

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

    public async Task<ServiceResult<ShareLinkDto>> CreateShareAsync(
        Guid fileId,
        Guid userId,
        CreateShareRequest request,
        CancellationToken cancellationToken = default)
    {
        var fileItem = await _db.FileItems
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.OwnerId == userId && f.GroupId == null && f.Active, cancellationToken);

        if (fileItem is null)
            return ServiceResultHelpers.NotFound<ShareLinkDto>(
                new NotFoundServiceError(ErrorKeys.Files.NotFoundMessage, ErrorKeys.Files.NotFound));

        return await CreateShareForFileAsync(fileItem, userId, request, cancellationToken);
    }

    public async Task<ServiceResult<ShareLinkDto>> CreateGroupShareAsync(
        Guid groupId,
        Guid fileId,
        Guid userId,
        CreateShareRequest request,
        CancellationToken cancellationToken = default)
    {
        var accessCheck = await EnsureGroupAccessAsync(groupId, userId, GroupAccessLevel.Write, cancellationToken);
        if (!accessCheck.IsSuccess)
            return (ServiceResult<ShareLinkDto>)accessCheck;

        var fileItem = await _db.FileItems
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.GroupId == groupId && f.Active, cancellationToken);

        if (fileItem is null)
            return ServiceResultHelpers.NotFound<ShareLinkDto>(
                new NotFoundServiceError(ErrorKeys.Files.NotFoundMessage, ErrorKeys.Files.NotFound));

        return await CreateShareForFileAsync(fileItem, userId, request, cancellationToken);
    }

    public async Task<ServiceResult> RevokeShareAsync(
        string token,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var share = await _db.FileShares
            .FirstOrDefaultAsync(s => s.Token == token && s.OwnerId == userId && s.Active, cancellationToken);

        if (share is null)
            return ServiceResult.NotFound(new NotFoundServiceError(
                ErrorKeys.Files.ShareNotFoundMessage,
                ErrorKeys.Files.ShareNotFound));

        share.Active = false;
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.NoContent();
    }

    public async Task<ServiceResult<SharePreviewDto>> GetSharePreviewAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveShareAsync(token, cancellationToken);
        if (!resolved.IsSuccess)
            return ServiceResult.FromFailed(resolved).ToType<SharePreviewDto>();

        var (share, fileItem) = resolved.Result!;

        return new SharePreviewDto
        {
            FileName = fileItem.Name,
            ContentType = fileItem.StorageObject?.ContentType,
            Size = fileItem.StorageObject?.Size,
            ExpiresAt = share.ExpiresAt,
            AccessLevel = share.AccessLevel
        };
    }

    public async Task<ServiceResult<FileDownloadResult>> GetShareDownloadAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveShareAsync(token, cancellationToken);
        if (!resolved.IsSuccess)
            return ServiceResult.FromFailed(resolved).ToType<FileDownloadResult>();

        var (_, fileItem) = resolved.Result!;

        if (fileItem.StorageObject is null)
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
            _logger.LogError(ex, "Failed to download shared file for token {Token}", token);
            return ServiceResultHelpers.InternalServerError<FileDownloadResult>(
                new ServiceError(ErrorKeys.Files.StorageFailedMessage, ErrorKeys.Files.StorageFailed, null));
        }
    }

    public async Task<ServiceResult<IReadOnlyList<FileItemDto>>> ListGroupAsync(
        Guid groupId,
        Guid? parentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var accessCheck = await EnsureGroupAccessAsync(groupId, userId, GroupAccessLevel.Read, cancellationToken);
        if (!accessCheck.IsSuccess)
            return (ServiceResult<IReadOnlyList<FileItemDto>>)accessCheck;

        if (parentId is Guid parentFolderId)
        {
            var parentCheck = await ValidateGroupParentFolderAsync(parentFolderId, groupId, cancellationToken);
            if (!parentCheck.IsSuccess)
                return (ServiceResult<IReadOnlyList<FileItemDto>>)parentCheck;
        }

        var accessLevel = await GetUserGroupAccessLevelAsync(groupId, userId, cancellationToken);

        var rows = await _db.FileItems
            .AsNoTracking()
            .Where(f => f.GroupId == groupId && f.Active && f.ParentId == parentId)
            .OrderByDescending(f => f.IsFolder)
            .ThenBy(f => f.Name)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.IsFolder,
                f.ParentId,
                f.OwnerId,
                Size = f.IsFolder ? (long?)null : f.StorageObject!.Size,
                ContentType = f.IsFolder ? null : f.StorageObject!.ContentType
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(f => new FileItemDto
        {
            Id = f.Id,
            Name = f.Name,
            IsFolder = f.IsFolder,
            ParentId = f.ParentId,
            Size = f.Size,
            ContentType = f.ContentType,
            CanDelete = CanDeleteGroupItem(accessLevel, f.OwnerId, userId)
        }).ToList();

        return items;
    }

    public async Task<ServiceResult<FileItemDto>> CreateGroupFolderAsync(
        Guid groupId,
        CreateFolderRequest request,
        Guid userId,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        var accessCheck = await EnsureGroupAccessAsync(groupId, userId, GroupAccessLevel.Write, cancellationToken);
        if (!accessCheck.IsSuccess)
            return (ServiceResult<FileItemDto>)accessCheck;

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
            var parentCheck = await ValidateGroupParentFolderAsync(parentId, groupId, cancellationToken);
            if (!parentCheck.IsSuccess)
                return (ServiceResult<FileItemDto>)parentCheck;
        }

        if (await NameExistsInGroupAsync(groupId, request.ParentId, name, cancellationToken))
            return ServiceResultHelpers.Conflict<FileItemDto>(
                new ValidationServiceError(ErrorKeys.Files.NameExistsMessage, ErrorKeys.Files.NameExists));

        var folder = FileItem.CreateFolder(name, request.ParentId, userId, groupId, userName);
        _db.FileItems.Add(folder);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(folder, null);
    }

    public async Task<ServiceResult<FileItemDto>> UploadGroupAsync(
        Guid groupId,
        Guid? parentId,
        string fileName,
        Stream content,
        long size,
        string? contentType,
        Guid userId,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        var accessCheck = await EnsureGroupAccessAsync(groupId, userId, GroupAccessLevel.Write, cancellationToken);
        if (!accessCheck.IsSuccess)
            return (ServiceResult<FileItemDto>)accessCheck;

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
            var parentCheck = await ValidateGroupParentFolderAsync(parentFolderId, groupId, cancellationToken);
            if (!parentCheck.IsSuccess)
                return (ServiceResult<FileItemDto>)parentCheck;
        }

        if (await NameExistsInGroupAsync(groupId, parentId, name, cancellationToken))
            return ServiceResultHelpers.Conflict<FileItemDto>(
                new ValidationServiceError(ErrorKeys.Files.NameExistsMessage, ErrorKeys.Files.NameExists));

        var storageObject = StorageObject.Create(size, contentType, createdBy: userName);
        var uploaded = false;

        try
        {
            await _storage.UploadAsync(storageObject.StorageKey, content, size, contentType, cancellationToken);
            uploaded = true;

            _db.StorageObjects.Add(storageObject);
            var fileItem = FileItem.CreateFile(name, parentId, userId, storageObject.Id, groupId, userName);
            _db.FileItems.Add(fileItem);
            await _db.SaveChangesAsync(cancellationToken);

            return ToDto(fileItem, storageObject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload group file {FileName} for group {GroupId}", name, groupId);

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

    public async Task<ServiceResult> DeleteGroupAsync(
        Guid groupId,
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.FileItems
            .FirstOrDefaultAsync(f => f.Id == id && f.GroupId == groupId && f.Active, cancellationToken);

        if (item is null)
            return ServiceResult.NotFound(new NotFoundServiceError(
                ErrorKeys.Files.NotFoundMessage,
                ErrorKeys.Files.NotFound));

        var requiredLevel = item.OwnerId == userId
            ? GroupAccessLevel.Write
            : GroupAccessLevel.Admin;

        var accessCheck = await EnsureGroupAccessAsync(groupId, userId, requiredLevel, cancellationToken);
        if (!accessCheck.IsSuccess)
            return accessCheck;

        var idsToDeactivate = new List<Guid> { item.Id };

        if (item.IsFolder)
        {
            var descendantIds = await CollectGroupDescendantIdsAsync(item.Id, groupId, cancellationToken);
            idsToDeactivate.AddRange(descendantIds);
        }

        var items = await _db.FileItems
            .Where(f => idsToDeactivate.Contains(f.Id) && f.GroupId == groupId && f.Active)
            .ToListAsync(cancellationToken);

        foreach (var fileItem in items)
            fileItem.Active = false;

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.NoContent();
    }

    public async Task<ServiceResult<FileDownloadResult>> GetGroupDownloadAsync(
        Guid groupId,
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var accessCheck = await EnsureGroupAccessAsync(groupId, userId, GroupAccessLevel.Read, cancellationToken);
        if (!accessCheck.IsSuccess)
            return (ServiceResult<FileDownloadResult>)accessCheck;

        var fileItem = await _db.FileItems
            .Include(f => f.StorageObject)
            .FirstOrDefaultAsync(f => f.Id == id && f.GroupId == groupId && f.Active, cancellationToken);

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
            _logger.LogError(ex, "Failed to download group file {FileId} for group {GroupId}", id, groupId);
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
            .Where(f => f.OwnerId == userId && f.GroupId == null && f.Active)
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

    private async Task<IReadOnlyList<Guid>> CollectGroupDescendantIdsAsync(
        Guid folderId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var allItems = await _db.FileItems
            .AsNoTracking()
            .Where(f => f.GroupId == groupId && f.Active)
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

    private async Task<GroupAccessLevel> GetUserGroupAccessLevelAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var membership = await _db.UserGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId, cancellationToken);

        return membership?.AccessLevel ?? GroupAccessLevel.Read;
    }

    private async Task<ServiceResult> EnsureGroupAccessAsync(
        Guid groupId,
        Guid userId,
        GroupAccessLevel requiredLevel,
        CancellationToken cancellationToken)
    {
        var groupExists = await _db.Groups.AnyAsync(g => g.Id == groupId && g.Active, cancellationToken);
        if (!groupExists)
            return ServiceResult.NotFound(new NotFoundServiceError(
                ErrorKeys.Groups.NotFoundMessage,
                ErrorKeys.Groups.NotFound));

        var membership = await _db.UserGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId, cancellationToken);

        if (membership is null)
            return ServiceResult.Forbidden(new ValidationServiceError(
                ErrorKeys.Groups.NotMemberMessage,
                ErrorKeys.Groups.NotMember));

        if (membership.AccessLevel < requiredLevel)
            return ServiceResult.Forbidden(new ValidationServiceError(
                ErrorKeys.Groups.InsufficientAccessMessage,
                ErrorKeys.Groups.InsufficientAccess));

        return ServiceResult.NoContent();
    }

    private static bool CanDeleteGroupItem(GroupAccessLevel level, Guid ownerId, Guid userId) =>
        level >= GroupAccessLevel.Admin || (level >= GroupAccessLevel.Write && ownerId == userId);

    private async Task<ServiceResult> ValidateGroupParentFolderAsync(
        Guid parentId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var parent = await _db.FileItems
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == parentId && f.GroupId == groupId && f.Active, cancellationToken);

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

    private Task<bool> NameExistsInGroupAsync(
        Guid groupId,
        Guid? parentId,
        string name,
        CancellationToken cancellationToken) =>
        _db.FileItems.AnyAsync(
            f => f.GroupId == groupId && f.Active && f.ParentId == parentId && f.Name == name,
            cancellationToken);

    private async Task<ServiceResult> ValidateParentFolderAsync(
        Guid parentId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var parent = await _db.FileItems
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == parentId && f.OwnerId == userId && f.GroupId == null && f.Active, cancellationToken);

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
            f => f.OwnerId == userId && f.GroupId == null && f.Active && f.ParentId == parentId && f.Name == name,
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

    private async Task<ServiceResult<ShareLinkDto>> CreateShareForFileAsync(
        FileItem fileItem,
        Guid userId,
        CreateShareRequest request,
        CancellationToken cancellationToken)
    {
        if (fileItem.IsFolder)
            return ServiceResultHelpers.BadRequest<ShareLinkDto>(
                new ValidationServiceError(ErrorKeys.Files.NotAFileMessage, ErrorKeys.Files.NotAFile));

        if (request.ExpiresAt is DateTime expiresAt && expiresAt <= DateTime.UtcNow)
            return ServiceResultHelpers.BadRequest<ShareLinkDto>(
                new ValidationServiceError(
                    ErrorKeys.Files.ShareInvalidExpiryMessage,
                    ErrorKeys.Files.ShareInvalidExpiry));

        var token = GenerateShareToken();
        var share = FileShareEntity.Create(
            fileItem.Id,
            userId,
            token,
            request.ExpiresAt,
            ShareAccessLevel.Read);

        _db.FileShares.Add(share);
        await _db.SaveChangesAsync(cancellationToken);

        return new ShareLinkDto
        {
            Token = share.Token,
            FileName = fileItem.Name,
            ExpiresAt = share.ExpiresAt,
            AccessLevel = share.AccessLevel
        };
    }

    private async Task<ServiceResult<(FileShareEntity Share, FileItem FileItem)>> ResolveShareAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var share = await _db.FileShares
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Token == token && s.Active, cancellationToken);

        if (share is null)
            return ServiceResultHelpers.NotFound<(FileShareEntity, FileItem)>(
                new NotFoundServiceError(
                    ErrorKeys.Files.ShareNotFoundMessage,
                    ErrorKeys.Files.ShareNotFound));

        if (share.ExpiresAt is DateTime expiresAt && expiresAt <= DateTime.UtcNow)
            return ServiceResultHelpers.NotFound<(FileShareEntity, FileItem)>(
                new NotFoundServiceError(
                    ErrorKeys.Files.ShareExpiredMessage,
                    ErrorKeys.Files.ShareExpired));

        var fileItem = await _db.FileItems
            .AsNoTracking()
            .Include(f => f.StorageObject)
            .FirstOrDefaultAsync(f => f.Id == share.FileItemId && f.Active && !f.IsFolder, cancellationToken);

        if (fileItem is null)
            return ServiceResultHelpers.NotFound<(FileShareEntity, FileItem)>(
                new NotFoundServiceError(
                    ErrorKeys.Files.ShareNotFoundMessage,
                    ErrorKeys.Files.ShareNotFound));

        return (share, fileItem);
    }

    private static string GenerateShareToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

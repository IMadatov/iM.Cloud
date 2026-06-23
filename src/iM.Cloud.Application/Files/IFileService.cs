using BaseCrud.ServiceResults;

namespace iM.Cloud.Application.Files;

public interface IFileService
{
    Task<ServiceResult<IReadOnlyList<FileItemDto>>> ListAsync(
        Guid? parentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<FileItemDto>> CreateFolderAsync(
        CreateFolderRequest request,
        Guid userId,
        string? userName,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<FileItemDto>> UploadAsync(
        Guid? parentId,
        string fileName,
        Stream content,
        long size,
        string? contentType,
        Guid userId,
        string? userName,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<FileDownloadResult>> GetDownloadAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ShareLinkDto>> CreateShareAsync(
        Guid fileId,
        Guid userId,
        CreateShareRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ShareLinkDto>> CreateGroupShareAsync(
        Guid groupId,
        Guid fileId,
        Guid userId,
        CreateShareRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> RevokeShareAsync(
        string token,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<SharePreviewDto>> GetSharePreviewAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<FileDownloadResult>> GetShareDownloadAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<FileItemDto>>> ListGroupAsync(
        Guid groupId,
        Guid? parentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<FileItemDto>> CreateGroupFolderAsync(
        Guid groupId,
        CreateFolderRequest request,
        Guid userId,
        string? userName,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<FileItemDto>> UploadGroupAsync(
        Guid groupId,
        Guid? parentId,
        string fileName,
        Stream content,
        long size,
        string? contentType,
        Guid userId,
        string? userName,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteGroupAsync(
        Guid groupId,
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<FileDownloadResult>> GetGroupDownloadAsync(
        Guid groupId,
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}

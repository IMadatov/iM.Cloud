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
}

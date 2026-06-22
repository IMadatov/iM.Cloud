namespace iM.Cloud.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task UploadAsync(string storageKey, Stream content, long size, string? contentType, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default);
}

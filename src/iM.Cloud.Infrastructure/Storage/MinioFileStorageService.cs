using iM.Cloud.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace iM.Cloud.Infrastructure.Storage;

public sealed class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _client;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioFileStorageService> _logger;
    private readonly SemaphoreSlim _bucketInitLock = new(1, 1);
    private bool _bucketEnsured;

    public MinioFileStorageService(IOptions<MinioOptions> options, ILogger<MinioFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("Minio:Endpoint is required.");
        if (string.IsNullOrWhiteSpace(_options.AccessKey))
            throw new InvalidOperationException("Minio:AccessKey is required.");
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new InvalidOperationException("Minio:SecretKey is required.");
        if (string.IsNullOrWhiteSpace(_options.BucketName))
            throw new InvalidOperationException("Minio:BucketName is required.");

        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
    }

    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        if (_bucketEnsured)
            return;

        await _bucketInitLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketEnsured)
                return;

            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.BucketName),
                cancellationToken);

            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_options.BucketName),
                    cancellationToken);
                _logger.LogInformation("Created MinIO bucket {BucketName}", _options.BucketName);
            }

            _bucketEnsured = true;
        }
        finally
        {
            _bucketInitLock.Release();
        }
    }

    public async Task UploadAsync(
        string storageKey,
        Stream content,
        long size,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var args = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storageKey)
            .WithStreamData(content)
            .WithObjectSize(size)
            .WithContentType(contentType ?? "application/octet-stream");

        await _client.PutObjectAsync(args, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var memoryStream = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storageKey)
            .WithCallbackStream(async (source, ct) =>
            {
                await source.CopyToAsync(memoryStream, ct);
            });

        await _client.GetObjectAsync(args, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var args = new RemoveObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storageKey);

        await _client.RemoveObjectAsync(args, cancellationToken);
    }
}

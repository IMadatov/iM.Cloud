namespace iM.Cloud.Application.Files;

public sealed class FileDownloadResult : IAsyncDisposable
{
    public Stream Content { get; init; } = null!;
    public string ContentType { get; init; } = "application/octet-stream";
    public string FileName { get; init; } = null!;

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

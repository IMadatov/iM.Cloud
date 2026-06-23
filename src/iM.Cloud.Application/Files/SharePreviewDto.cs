using iM.Cloud.Domain.Authorization;

namespace iM.Cloud.Application.Files;

public sealed class SharePreviewDto
{
    public string FileName { get; set; } = null!;
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public ShareAccessLevel AccessLevel { get; set; }
}

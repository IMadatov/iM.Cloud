using iM.Cloud.Domain.Authorization;

namespace iM.Cloud.Application.Files;

public sealed class ShareLinkDto
{
    public string Token { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
    public ShareAccessLevel AccessLevel { get; set; }
}

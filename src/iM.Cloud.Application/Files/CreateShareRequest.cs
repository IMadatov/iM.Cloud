using iM.Cloud.Domain.Authorization;

namespace iM.Cloud.Application.Files;

public sealed class CreateShareRequest
{
    public DateTime? ExpiresAt { get; set; }
    public ShareAccessLevel? AccessLevel { get; set; }
}

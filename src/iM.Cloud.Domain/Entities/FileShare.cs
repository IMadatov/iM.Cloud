using BaseCrud.Abstractions.Entities;
using iM.Cloud.Domain.Authorization;

namespace iM.Cloud.Domain.Entities;

public class FileShare : EntityBase<Guid>
{
    public string Token { get; set; } = null!;
    public Guid FileItemId { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public ShareAccessLevel AccessLevel { get; set; }

    public FileShare() { }

    public static FileShare Create(
        Guid fileItemId,
        Guid ownerId,
        string token,
        DateTime? expiresAt = null,
        ShareAccessLevel accessLevel = ShareAccessLevel.Read)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Share token is required.", nameof(token));

        return new FileShare
        {
            Id = Guid.NewGuid(),
            Token = token.Trim(),
            FileItemId = fileItemId,
            OwnerId = ownerId,
            ExpiresAt = expiresAt,
            AccessLevel = accessLevel,
            Active = true,
            CreatedDate = DateTime.UtcNow
        };
    }
}

using BaseCrud.Abstractions.Entities;

namespace iM.Cloud.Domain.Entities;

public class RefreshToken : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            Active = true,
            CreatedDate = DateTime.UtcNow
        };
    }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow && Active;

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
        Active = false;
    }
}

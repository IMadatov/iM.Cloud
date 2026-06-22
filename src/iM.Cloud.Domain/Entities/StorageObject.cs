using BaseCrud.Abstractions.Entities;

namespace iM.Cloud.Domain.Entities;

public class StorageObject : EntityBase<Guid>
{
    public string StorageKey { get; set; } = null!;
    public long Size { get; set; }
    public string? ContentType { get; set; }
    public string? Sha256 { get; set; }

    public StorageObject() { }

    public static StorageObject Create(long size, string? contentType, string? sha256 = null, string? createdBy = null)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size cannot be negative.");

        var id = Guid.NewGuid();
        return new StorageObject
        {
            Id = id,
            StorageKey = $"blobs/{id}",
            Size = size,
            ContentType = contentType,
            Sha256 = sha256,
            Active = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }
}

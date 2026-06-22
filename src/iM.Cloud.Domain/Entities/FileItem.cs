using BaseCrud.Abstractions.Entities;

namespace iM.Cloud.Domain.Entities;

public class FileItem : EntityBase<Guid>
{
    public const int MaxNameLength = 255;

    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public bool IsFolder { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? StorageObjectId { get; set; }

    public StorageObject? StorageObject { get; set; }

    public FileItem() { }

    public static FileItem CreateFolder(string name, Guid? parentId, Guid ownerId, string? createdBy = null)
    {
        ValidateName(name);

        return new FileItem
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            ParentId = parentId,
            IsFolder = true,
            OwnerId = ownerId,
            StorageObjectId = null,
            Active = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public static FileItem CreateFile(
        string name,
        Guid? parentId,
        Guid ownerId,
        Guid storageObjectId,
        string? createdBy = null)
    {
        ValidateName(name);

        return new FileItem
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            ParentId = parentId,
            IsFolder = false,
            OwnerId = ownerId,
            StorageObjectId = storageObjectId,
            Active = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("File name is required.", nameof(name));

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"File name cannot exceed {MaxNameLength} characters.", nameof(name));

        if (trimmed.Any(char.IsControl))
            throw new ArgumentException("File name cannot contain control characters.", nameof(name));
    }
}

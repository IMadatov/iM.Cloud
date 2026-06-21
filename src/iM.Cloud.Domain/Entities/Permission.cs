using BaseCrud.Abstractions.Entities;

namespace iM.Cloud.Domain.Entities;

public class Permission : EntityBase<Guid>
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public Permission() { }

    public static Permission Create(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name is required.", nameof(name));

        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Active = true,
            CreatedDate = DateTime.UtcNow
        };
    }
}

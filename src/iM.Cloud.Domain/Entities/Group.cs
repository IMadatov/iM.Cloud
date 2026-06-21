using BaseCrud.Abstractions.Entities;

namespace iM.Cloud.Domain.Entities;

public class Group : EntityBase<Guid>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public Group() { }

    public static Group Create(string name, string? description = null, string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.", nameof(name));

        return new Group
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Active = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }
}

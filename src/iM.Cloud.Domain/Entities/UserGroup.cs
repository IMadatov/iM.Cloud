using BaseCrud.Abstractions.Entities;

namespace iM.Cloud.Domain.Entities;

public class UserGroup : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }

    public UserGroup() { }

    public static UserGroup Create(Guid userId, Guid groupId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        if (groupId == Guid.Empty)
            throw new ArgumentException("Group id is required.", nameof(groupId));

        return new UserGroup
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GroupId = groupId,
            Active = true,
            CreatedDate = DateTime.UtcNow
        };
    }
}

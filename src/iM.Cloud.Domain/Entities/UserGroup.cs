using BaseCrud.Abstractions.Entities;
using iM.Cloud.Domain.Authorization;

namespace iM.Cloud.Domain.Entities;

public class UserGroup : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public GroupAccessLevel AccessLevel { get; set; }

    public UserGroup() { }

    public static UserGroup Create(
        Guid userId,
        Guid groupId,
        GroupAccessLevel accessLevel = GroupAccessLevel.Write)
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
            AccessLevel = accessLevel,
            Active = true,
            CreatedDate = DateTime.UtcNow
        };
    }
}

using BaseCrud.Abstractions.Services;
using BaseCrud.ServiceResults;
using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Dtos.Groups;
using iM.Cloud.Domain.Entities;

namespace iM.Cloud.Application.Admin.Groups;

public interface IGroupService : ICrudService<Group, GroupListDto, GroupDetailsDto, Guid, Guid>
{
    Task<ServiceResult> AddMemberAsync(
        Guid groupId,
        Guid userId,
        GroupAccessLevel accessLevel = GroupAccessLevel.Write,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateMemberAccessAsync(
        Guid groupId,
        Guid userId,
        GroupAccessLevel accessLevel,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<GroupMemberDto>>> ListMembersAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<GroupListDto>>> GetMyGroupsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

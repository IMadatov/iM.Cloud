using BaseCrud.Abstractions.Entities;
using BaseCrud.PrimeNg;
using iM.Cloud.API.Authorization;
using iM.Cloud.API.Common;
using iM.Cloud.Application.Admin.Groups;
using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Dtos.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iM.Cloud.API.Controllers;

[Authorize]
[Route("api/admin/groups")]
public sealed class AdminGroupsController : ApiControllerBase
{
    private readonly IGroupService _groupService;

    public AdminGroupsController(IGroupService groupService, ICurrentUserService currentUser)
        : base(currentUser)
        => _groupService = groupService;

    [HttpPost("GetAll")]
    [RequirePermission(PermissionCodes.GroupsRead)]
    public Task<ActionResult<QueryResult<GroupListDto>?>> GetAll([FromBody] PrimeTableMetaData metaData)
        => FromServiceResult(_groupService.GetAllAsync(metaData, UserProfile));

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.GroupsRead)]
    public Task<ActionResult<GroupDetailsDto?>> GetById(Guid id, CancellationToken cancellationToken)
        => FromServiceResult(_groupService.GetByIdAsync(id, UserProfile, cancellationToken: cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.GroupsManage)]
    public Task<ActionResult<GroupDetailsDto?>> Create([FromBody] GroupDetailsDto request, CancellationToken cancellationToken)
        => FromServiceResult(_groupService.InsertAsync(request, UserProfile, cancellationToken));

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCodes.GroupsManage)]
    public Task<ActionResult<GroupDetailsDto?>> Update(Guid id, [FromBody] GroupDetailsDto request, CancellationToken cancellationToken)
    {
        request.Id = id;
        return FromServiceResult(_groupService.UpdateAsync(request, UserProfile, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.GroupsManage)]
    public Task<ActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        => FromServiceResult(_groupService.DeactivateByIdAsync(id, UserProfile, cancellationToken: cancellationToken));

    [HttpPost("{groupId:guid}/members")]
    [RequirePermission(PermissionCodes.GroupsManage)]
    public Task<ActionResult> AddMember(Guid groupId, [FromBody] AddGroupMemberRequest request, CancellationToken cancellationToken)
        => FromServiceResult(_groupService.AddMemberAsync(
            groupId,
            request.UserId,
            request.AccessLevel ?? GroupAccessLevel.Write,
            cancellationToken));

    [HttpPut("{groupId:guid}/members/{userId:guid}/access")]
    [RequirePermission(PermissionCodes.GroupsManage)]
    public Task<ActionResult> UpdateMemberAccess(
        Guid groupId,
        Guid userId,
        [FromBody] UpdateGroupMemberAccessRequest request,
        CancellationToken cancellationToken)
        => FromServiceResult(_groupService.UpdateMemberAccessAsync(
            groupId,
            userId,
            request.AccessLevel,
            cancellationToken));

    [HttpDelete("{groupId:guid}/members/{userId:guid}")]
    [RequirePermission(PermissionCodes.GroupsManage)]
    public Task<ActionResult> RemoveMember(Guid groupId, Guid userId, CancellationToken cancellationToken)
        => FromServiceResult(_groupService.RemoveMemberAsync(groupId, userId, cancellationToken));

    [HttpGet("{groupId:guid}/members")]
    [RequirePermission(PermissionCodes.GroupsRead)]
    public Task<ActionResult<IReadOnlyList<GroupMemberDto>?>> ListMembers(Guid groupId, CancellationToken cancellationToken)
        => FromServiceResult(_groupService.ListMembersAsync(groupId, cancellationToken));
}

[Authorize]
[Route("api/groups")]
public sealed class GroupsController : ApiControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ICurrentUserService _currentUser;

    public GroupsController(IGroupService groupService, ICurrentUserService currentUser)
        : base(currentUser)
    {
        _groupService = groupService;
        _currentUser = currentUser;
    }

    [HttpGet("mine")]
    public Task<ActionResult<IReadOnlyList<GroupListDto>?>> Mine(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Task.FromResult<ActionResult<IReadOnlyList<GroupListDto>?>>(Unauthorized());

        return FromServiceResult(_groupService.GetMyGroupsAsync(userId, cancellationToken));
    }
}

public sealed class AddGroupMemberRequest
{
    public Guid UserId { get; set; }
    public GroupAccessLevel? AccessLevel { get; set; }
}

public sealed class UpdateGroupMemberAccessRequest
{
    public GroupAccessLevel AccessLevel { get; set; }
}

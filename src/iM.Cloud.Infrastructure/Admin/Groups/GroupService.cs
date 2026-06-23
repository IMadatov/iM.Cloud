using AutoMapper;
using BaseCrud.Abstractions.Entities;
using BaseCrud.EntityFrameworkCore;
using BaseCrud.Errors;
using BaseCrud.ServiceResults;
using iM.Cloud.Application.Admin.Groups;
using iM.Cloud.Application.Common;
using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Dtos.Groups;
using iM.Cloud.Domain.Entities;
using iM.Cloud.Domain.Mappings;
using iM.Cloud.Infrastructure.Identity;
using iM.Cloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iM.Cloud.Infrastructure.Admin.Groups;

public sealed class GroupService : BaseCrudService<Group, GroupListDto, GroupDetailsDto, Guid, Guid>, IGroupService
{
    private readonly ApplicationDbContext _db;

    public GroupService(ApplicationDbContext dbContext, IMapper mapper)
        : base(dbContext, mapper)
        => _db = dbContext;

    public override async Task<ServiceResult<GroupDetailsDto?>> GetByIdAsync(
        Guid id,
        IUserProfile<Guid>? userProfile,
        Func<CrudActionContext<Group, Guid, Guid>, ValueTask<IQueryable<Group>>>? customAction = null,
        CancellationToken cancellationToken = default)
    {
        var entityResult = await GetEntityByIdAsync(id, userProfile, customAction, cancellationToken);
        if (!entityResult.IsSuccess)
            return ServiceResult.FromFailed(entityResult).ToType<GroupDetailsDto?>();

        if (entityResult.Result is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Db.NotFoundByIdMessage,
                ErrorKeys.Db.NotFoundById));

        return GroupMappings.ToDetailsDto(entityResult.Result);
    }

    public override async Task<ServiceResult<GroupDetailsDto>> InsertAsync(
        GroupDetailsDto entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
    {
        var name = entity.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.NameRequiredMessage,
                ErrorKeys.Validation.NameRequired));

        var exists = await _db.Groups.AnyAsync(g => g.Name == name, cancellationToken);
        if (exists)
            return Conflict(new ValidationServiceError(
                ErrorKeys.Validation.NameExistsMessage,
                ErrorKeys.Validation.NameExists));

        var group = Group.Create(name, entity.Description, userProfile?.UserName);
        _db.Groups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);

        return GroupMappings.ToDetailsDto(group);
    }

    public override async Task<ServiceResult<GroupDetailsDto>> UpdateAsync(
        GroupDetailsDto entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == entity.Id, cancellationToken);
        if (group is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Db.NotFoundByIdMessage,
                ErrorKeys.Db.NotFoundById));

        var name = entity.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new ValidationServiceError(
                ErrorKeys.Validation.NameRequiredMessage,
                ErrorKeys.Validation.NameRequired));

        var nameTaken = await _db.Groups.AnyAsync(g => g.Id != entity.Id && g.Name == name, cancellationToken);
        if (nameTaken)
            return Conflict(new ValidationServiceError(
                ErrorKeys.Validation.NameExistsMessage,
                ErrorKeys.Validation.NameExists));

        GroupMappings.ApplyDetails(group, entity);
        group.LastModifiedBy = userProfile?.UserName;

        await _db.SaveChangesAsync(cancellationToken);
        return GroupMappings.ToDetailsDto(group);
    }

    public async Task<ServiceResult> AddMemberAsync(
        Guid groupId,
        Guid userId,
        GroupAccessLevel accessLevel = GroupAccessLevel.Write,
        CancellationToken cancellationToken = default)
    {
        var groupExists = await _db.Groups.AnyAsync(g => g.Id == groupId && g.Active, cancellationToken);
        if (!groupExists)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Groups.NotFoundMessage,
                ErrorKeys.Groups.NotFound));

        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Groups.UserNotFoundMessage,
                ErrorKeys.Groups.UserNotFound));

        var existing = await _db.UserGroups
            .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId, cancellationToken);

        if (existing is null)
        {
            _db.UserGroups.Add(UserGroup.Create(userId, groupId, accessLevel));
            await _db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    public async Task<ServiceResult> UpdateMemberAccessAsync(
        Guid groupId,
        Guid userId,
        GroupAccessLevel accessLevel,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.UserGroups
            .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId, cancellationToken);

        if (entry is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Groups.MembershipNotFoundMessage,
                ErrorKeys.Groups.MembershipNotFound));

        entry.AccessLevel = accessLevel;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    public async Task<ServiceResult> RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entry = await _db.UserGroups
            .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId, cancellationToken);

        if (entry is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Groups.MembershipNotFoundMessage,
                ErrorKeys.Groups.MembershipNotFound));

        _db.UserGroups.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    public async Task<ServiceResult<IReadOnlyList<GroupMemberDto>>> ListMembersAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var groupExists = await _db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken);
        if (!groupExists)
            return (ServiceResult<IReadOnlyList<GroupMemberDto>>)NotFound(new NotFoundServiceError(
                ErrorKeys.Groups.NotFoundMessage,
                ErrorKeys.Groups.NotFound));

        var members = await _db.UserGroups
            .Where(ug => ug.GroupId == groupId)
            .Join(
                _db.Users,
                ug => ug.UserId,
                u => u.Id,
                (ug, u) => new GroupMemberDto
                {
                    UserId = u.Id,
                    Email = u.Email ?? u.UserName ?? string.Empty,
                    DisplayName = ((ApplicationUser)u).DisplayName,
                    AccessLevel = ug.AccessLevel
                })
            .OrderBy(m => m.Email)
            .ToListAsync(cancellationToken);

        return members;
    }

    public async Task<ServiceResult<IReadOnlyList<GroupListDto>>> GetMyGroupsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var groups = await _db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Join(_db.Groups.Where(g => g.Active), ug => ug.GroupId, g => g.Id, (ug, g) => new { ug, g })
            .OrderBy(x => x.g.Name)
            .Select(x => new GroupListDto
            {
                Id = x.g.Id,
                Name = x.g.Name,
                Description = x.g.Description,
                CreatedAt = x.g.CreatedDate,
                Active = x.g.Active,
                AccessLevel = x.ug.AccessLevel
            })
            .ToListAsync(cancellationToken);

        return groups;
    }
}

using AutoMapper;
using BaseCrud.Abstractions.Entities;
using BaseCrud.EntityFrameworkCore;
using BaseCrud.Errors;
using BaseCrud.ServiceResults;
using iM.Cloud.Application.Common;
using iM.Cloud.Application.Admin.Permissions;
using iM.Cloud.Domain.Dtos.Permissions;
using iM.Cloud.Domain.Entities;
using iM.Cloud.Domain.Mappings;

using iM.Cloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iM.Cloud.Infrastructure.Admin.Permissions;

public sealed class PermissionService
    : BaseCrudService<Permission, PermissionListDto, PermissionDetailsDto, Guid, Guid>, IPermissionService
{
    public PermissionService(ApplicationDbContext dbContext, IMapper mapper)
        : base(dbContext, mapper)
    {
    }

    public override async Task<ServiceResult<PermissionDetailsDto?>> GetByIdAsync(
        Guid id,
        IUserProfile<Guid>? userProfile,
        Func<CrudActionContext<Permission, Guid, Guid>, ValueTask<IQueryable<Permission>>>? customAction = null,
        CancellationToken cancellationToken = default)
    {
        var entityResult = await GetEntityByIdAsync(id, userProfile, customAction, cancellationToken);
        if (!entityResult.IsSuccess)
            return ServiceResult.FromFailed(entityResult).ToType<PermissionDetailsDto?>();

        if (entityResult.Result is null)
            return NotFound(new NotFoundServiceError(
                ErrorKeys.Db.NotFoundByIdMessage,
                ErrorKeys.Db.NotFoundById));

        return PermissionMappings.ToDetailsDto(entityResult.Result);
    }

    public override Task<ServiceResult<PermissionDetailsDto>> InsertAsync(
        PermissionDetailsDto entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ServiceResult<PermissionDetailsDto>>(
            Forbidden(new ValidationServiceError(
                ErrorKeys.Permissions.ReadOnlyMessage,
                ErrorKeys.Permissions.ReadOnly)));

    public override Task<ServiceResult<Permission>> InsertAsync(
        Permission entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ServiceResult<Permission>>(
            Forbidden(new ValidationServiceError(
                ErrorKeys.Permissions.ReadOnlyMessage,
                ErrorKeys.Permissions.ReadOnly)));

    public override Task<ServiceResult<PermissionDetailsDto>> UpdateAsync(
        PermissionDetailsDto entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ServiceResult<PermissionDetailsDto>>(
            Forbidden(new ValidationServiceError(
                ErrorKeys.Permissions.ReadOnlyMessage,
                ErrorKeys.Permissions.ReadOnly)));

    public override Task<ServiceResult<Permission>> UpdateAsync(
        Permission entity,
        IUserProfile<Guid>? userProfile,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ServiceResult<Permission>>(
            Forbidden(new ValidationServiceError(
                ErrorKeys.Permissions.ReadOnlyMessage,
                ErrorKeys.Permissions.ReadOnly)));
}

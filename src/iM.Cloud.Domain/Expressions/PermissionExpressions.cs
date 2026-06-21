using System.Linq.Expressions;
using BaseCrud.Abstractions.Entities;
using BaseCrud.Abstractions.Expressions;
using BaseCrud.Entities;
using BaseCrud.Expressions;
using BaseCrud.Expressions.Filter;
using iM.Cloud.Domain.Dtos.Permissions;
using iM.Cloud.Domain.Entities;

namespace iM.Cloud.Domain.Expressions;

public sealed class PermissionExpressions :
    ISelectExpression<Permission, PermissionListDto, Guid>,
    IGlobalFilterExpression<Permission, Guid>,
    IFilterExpression<Permission>
{
    public Expression<Func<Permission, PermissionListDto>> SelectExpression =>
        permission => new PermissionListDto
        {
            Id = permission.Id,
            Code = permission.Code,
            Name = permission.Name,
            Description = permission.Description,
            Active = permission.Active
        };

    public Expression<Func<Permission, bool>> GlobalSearchExpression(string strSearch) =>
        permission => permission.Code.Contains(strSearch)
            || permission.Name.Contains(strSearch)
            || (permission.Description != null && permission.Description.Contains(strSearch));

    public Func<FilterExpressions<Permission>, FilterExpressions<Permission>> FilterExpressions =>
        expressions => expressions
            .ForProperty(
                permission => permission.Code,
                builder => builder.HasFilter(
                    (permission, value) => value != null && permission.Code.Contains(value),
                    when: ExpressionConstraintsEnum.Contains));
}

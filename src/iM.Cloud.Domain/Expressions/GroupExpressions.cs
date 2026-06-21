using System.Linq.Expressions;
using BaseCrud.Abstractions.Entities;
using BaseCrud.Abstractions.Expressions;
using BaseCrud.Entities;
using BaseCrud.Expressions;
using BaseCrud.Expressions.Filter;
using iM.Cloud.Domain.Dtos.Groups;
using iM.Cloud.Domain.Entities;

namespace iM.Cloud.Domain.Expressions;

public sealed class GroupExpressions :
    ISelectExpression<Group, GroupListDto, Guid>,
    IGlobalFilterExpression<Group, Guid>,
    IFilterExpression<Group>
{
    public Expression<Func<Group, GroupListDto>> SelectExpression =>
        group => new GroupListDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CreatedAt = group.CreatedDate,
            Active = group.Active
        };

    public Expression<Func<Group, bool>> GlobalSearchExpression(string strSearch) =>
        group => group.Name.Contains(strSearch)
            || (group.Description != null && group.Description.Contains(strSearch));

    public Func<FilterExpressions<Group>, FilterExpressions<Group>> FilterExpressions =>
        expressions => expressions
            .ForProperty(
                group => group.Name,
                builder => builder.HasFilter(
                    (group, value) => value != null && group.Name.Contains(value),
                    when: ExpressionConstraintsEnum.Contains))
            .ForProperty(
                group => group.Active,
                builder => builder.HasFilter(
                    (group, value) => group.Active == value,
                    when: ExpressionConstraintsEnum.Equals));
}

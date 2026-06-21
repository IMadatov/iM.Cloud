using System.Linq.Expressions;
using BaseCrud.Abstractions.Entities;
using BaseCrud.Abstractions.Expressions;
using BaseCrud.Entities;
using BaseCrud.Expressions;
using BaseCrud.Expressions.Filter;
using iM.Cloud.Infrastructure.Dtos.Roles;
using iM.Cloud.Infrastructure.Identity;

namespace iM.Cloud.Infrastructure.Expressions;

public sealed class RoleExpressions :
    ISelectExpression<ApplicationRole, RoleListDto, Guid>,
    IGlobalFilterExpression<ApplicationRole, Guid>,
    IFilterExpression<ApplicationRole>
{
    public Expression<Func<ApplicationRole, RoleListDto>> SelectExpression =>
        role => new RoleListDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            Active = role.Active
        };

    public Expression<Func<ApplicationRole, bool>> GlobalSearchExpression(string strSearch) =>
        role => role.Name != null && role.Name.Contains(strSearch);

    public Func<FilterExpressions<ApplicationRole>, FilterExpressions<ApplicationRole>> FilterExpressions =>
        expressions => expressions
            .ForProperty(
                role => role.Name,
                builder => builder.HasFilter(
                    (role, value) => value != null && role.Name != null && role.Name.Contains(value),
                    when: ExpressionConstraintsEnum.Contains));
}

using System.Linq.Expressions;
using BaseCrud.Abstractions.Entities;
using BaseCrud.Abstractions.Expressions;
using BaseCrud.Entities;
using BaseCrud.Expressions;
using BaseCrud.Expressions.Filter;
using iM.Cloud.Infrastructure.Dtos.Users;
using iM.Cloud.Infrastructure.Identity;

namespace iM.Cloud.Infrastructure.Expressions;

public sealed class UserExpressions :
    ISelectExpression<ApplicationUser, UserListDto, Guid>,
    IGlobalFilterExpression<ApplicationUser, Guid>,
    IFilterExpression<ApplicationUser>
{
    public Expression<Func<ApplicationUser, UserListDto>> SelectExpression =>
        user => new UserListDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Active = user.Active,
            CreatedAt = user.CreatedDate ?? user.CreatedAt
        };

    public Expression<Func<ApplicationUser, bool>> GlobalSearchExpression(string strSearch) =>
        user => (user.Email != null && user.Email.Contains(strSearch))
            || user.DisplayName.Contains(strSearch);

    public Func<FilterExpressions<ApplicationUser>, FilterExpressions<ApplicationUser>> FilterExpressions =>
        expressions => expressions
            .ForProperty(
                user => user.Email,
                builder => builder.HasFilter(
                    (user, value) => value != null && user.Email != null && user.Email.Contains(value),
                    when: ExpressionConstraintsEnum.Contains))
            .ForProperty(
                user => user.Active,
                builder => builder.HasFilter(
                    (user, value) => user.Active == value,
                    when: ExpressionConstraintsEnum.Equals));
}

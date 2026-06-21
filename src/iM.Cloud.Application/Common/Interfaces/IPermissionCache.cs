namespace iM.Cloud.Application.Common.Interfaces;

public interface IPermissionCache
{
    Task<HashSet<string>> GetOrLoadAsync(Guid userId, CancellationToken cancellationToken = default);
    void InvalidateUser(Guid userId);
    Task InvalidateUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}

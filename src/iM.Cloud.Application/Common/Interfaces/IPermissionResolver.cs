namespace iM.Cloud.Application.Common.Interfaces;

public interface IPermissionResolver
{
    Task<HashSet<string>> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
}

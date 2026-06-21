using BaseCrud.Abstractions.Entities;

namespace iM.Cloud.API.Common;

public sealed class ImCloudUserProfile : IUserProfile<Guid>
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Fullname { get; set; }
}

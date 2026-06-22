namespace iM.Cloud.Application.Files;

public sealed class CreateFolderRequest
{
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
}

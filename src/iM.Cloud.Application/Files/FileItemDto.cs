namespace iM.Cloud.Application.Files;

public sealed class FileItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsFolder { get; set; }
    public Guid? ParentId { get; set; }
    public long? Size { get; set; }
    public string? ContentType { get; set; }
    public bool? CanDelete { get; set; }
}

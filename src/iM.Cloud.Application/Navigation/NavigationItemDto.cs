namespace iM.Cloud.Application.Navigation;

public sealed class NavigationItemDto
{
    public string Key { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string Path { get; set; } = null!;
    public int Order { get; set; }
}

namespace AtEase.App.Models;

public class LauncherPage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = string.Empty;

    public List<string> ItemIds { get; set; } = [];
}

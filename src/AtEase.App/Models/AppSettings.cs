namespace AtEase.App.Models;

public class AppSettings
{
    public int Version { get; set; } = 1;

    public List<AppItem> Apps { get; set; } = [];

    public List<FolderItem> Folders { get; set; } = [];

    public List<LauncherPage> Pages { get; set; } = [];

    public DesktopBrowsingSettings DesktopBrowsing { get; set; } = new();
}

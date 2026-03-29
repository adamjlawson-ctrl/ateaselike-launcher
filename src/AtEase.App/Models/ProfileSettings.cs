namespace AtEase.App.Models;

public class ProfileSettings
{
    public const string LauncherSectionApps = "apps";
    public const string LauncherSectionFolders = "folders";

    public int SchemaVersion { get; set; }

    public string ProfileName { get; set; } = "CurrentUser";

    public bool ShowFolders { get; set; } = true;

    public string DefaultPageId { get; set; } = "main";

    public string SelectedLauncherSection { get; set; } = LauncherSectionApps;

    public List<AppItem> AppTiles { get; set; } = [];

    public List<FolderItem> FolderTiles { get; set; } = [];
}

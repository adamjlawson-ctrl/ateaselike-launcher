namespace AtEase.App.Models;

public class ProfileSettings
{
    public int SchemaVersion { get; set; }

    public string ProfileName { get; set; } = "CurrentUser";

    public bool ShowFolders { get; set; } = true;

    public string DefaultPageId { get; set; } = "main";

    public List<AppItem> AppTiles { get; set; } = [];

    public List<FolderItem> FolderTiles { get; set; } = [];
}

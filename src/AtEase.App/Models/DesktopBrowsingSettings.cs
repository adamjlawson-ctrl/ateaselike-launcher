namespace AtEase.App.Models;

public class DesktopBrowsingSettings
{
    public bool IsEnabled { get; set; }

    public string RootFolder { get; set; } = string.Empty;

    public bool ShowHiddenItems { get; set; }
}

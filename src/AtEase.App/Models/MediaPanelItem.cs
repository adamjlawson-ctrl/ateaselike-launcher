namespace AtEase.App.Models;

public class MediaPanelItem
{
    public string DisplayName { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string IconHint { get; set; } = string.Empty;

    public bool IsFolder { get; set; }

    public bool IsParentNavigation { get; set; }
}

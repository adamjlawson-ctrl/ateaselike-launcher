namespace AtEase.App.Models;

public class AppItem : LauncherItem
{
    private string _arguments = string.Empty;

    public string Arguments
    {
        get => _arguments;
        set => SetProperty(ref _arguments, value);
    }

    public AppItem()
    {
        ItemType = LauncherItemType.App;
    }
}

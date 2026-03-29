using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace AtEase.App.Models;

public class LauncherItem : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private LauncherItemType _itemType;
    private string _displayName = string.Empty;
    private string _path = string.Empty;
    private string _iconHint = string.Empty;
    private bool _isVisible = true;
    private int _sortOrder;
    private string _pathHint = string.Empty;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public LauncherItemType ItemType
    {
        get => _itemType;
        set => SetProperty(ref _itemType, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string Path
    {
        get => _path;
        set => SetProperty(ref _path, value);
    }

    public string IconHint
    {
        get => _iconHint;
        set => SetProperty(ref _iconHint, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public int SortOrder
    {
        get => _sortOrder;
        set => SetProperty(ref _sortOrder, value);
    }

    [JsonIgnore]
    public string PathHint
    {
        get => _pathHint;
        set => SetProperty(ref _pathHint, value);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace AtEase.App.Models;

public partial class RemovableMediaPanel : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string RootPath { get; set; } = string.Empty;

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private bool _isActive;
}

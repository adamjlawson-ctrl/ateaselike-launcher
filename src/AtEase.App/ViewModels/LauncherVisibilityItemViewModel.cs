using CommunityToolkit.Mvvm.ComponentModel;

namespace AtEase.App.ViewModels;

public partial class LauncherVisibilityItemViewModel : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    private bool isVisible;
}

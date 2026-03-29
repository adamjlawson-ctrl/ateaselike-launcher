using CommunityToolkit.Mvvm.Input;

namespace AtEase.App.ViewModels;

public partial class TileViewModel : ViewModelBase
{
    public string DisplayName { get; }

    public string Path { get; }

    public string IconHint { get; }

    public IRelayCommand LaunchCommand { get; }

    public TileViewModel(string displayName, string path, string iconHint, IRelayCommand launchCommand)
    {
        DisplayName = displayName;
        Path = path;
        IconHint = iconHint;
        LaunchCommand = launchCommand;
    }
}

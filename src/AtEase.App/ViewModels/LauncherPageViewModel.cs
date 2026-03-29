using System.Collections.ObjectModel;

namespace AtEase.App.ViewModels;

public partial class LauncherPageViewModel : ViewModelBase
{
    public string Title { get; set; } = string.Empty;

    public ObservableCollection<TileViewModel> Items { get; } = [];
}

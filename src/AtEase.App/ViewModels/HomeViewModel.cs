using System.Collections.ObjectModel;
using AtEase.App.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace AtEase.App.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILauncherItemService _launcherItemService;
    private readonly IAppLaunchService _appLaunchService;

    public ObservableCollection<TileViewModel> Tiles { get; } = [];

    public HomeViewModel(
        ISettingsService settingsService,
        ILauncherItemService launcherItemService,
        IAppLaunchService appLaunchService)
    {
        _settingsService = settingsService;
        _launcherItemService = launcherItemService;
        _appLaunchService = appLaunchService;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var settings = await _settingsService.LoadAsync();
        var visibleItems = _launcherItemService.BuildVisibleItems(settings);

        Tiles.Clear();
        foreach (var item in visibleItems)
        {
            var command = new RelayCommand(() => _appLaunchService.TryLaunch(item.Path));
            Tiles.Add(new TileViewModel(item.DisplayName, item.Path, item.IconHint, command));
        }
    }
}

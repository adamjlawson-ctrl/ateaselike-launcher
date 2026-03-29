using System.Collections.ObjectModel;
using AtEase.App.Models;
using AtEase.App.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtEase.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private AppSettings _settings = new();

    [ObservableProperty]
    private string statusMessage = "Ready";

    public ObservableCollection<LauncherVisibilityItemViewModel> VisibilityItems { get; } = [];

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        _settings = await _settingsService.LoadAsync();

        VisibilityItems.Clear();

        foreach (var app in _settings.Apps.OrderBy(a => a.SortOrder))
        {
            VisibilityItems.Add(new LauncherVisibilityItemViewModel
            {
                Id = app.Id,
                DisplayName = app.DisplayName,
                IsVisible = app.IsVisible
            });
        }

        foreach (var folder in _settings.Folders.OrderBy(f => f.SortOrder))
        {
            VisibilityItems.Add(new LauncherVisibilityItemViewModel
            {
                Id = folder.Id,
                DisplayName = folder.DisplayName,
                IsVisible = folder.IsVisible
            });
        }

        StatusMessage = "Settings loaded.";
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        foreach (var item in VisibilityItems)
        {
            var app = _settings.Apps.FirstOrDefault(a => a.Id == item.Id);
            if (app is not null)
            {
                app.IsVisible = item.IsVisible;
                continue;
            }

            var folder = _settings.Folders.FirstOrDefault(f => f.Id == item.Id);
            if (folder is not null)
            {
                folder.IsVisible = item.IsVisible;
            }
        }

        await _settingsService.SaveAsync(_settings);
        StatusMessage = "Settings saved.";
    }
}

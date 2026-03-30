using System.Collections.ObjectModel;
using AtEase.App.Models;

namespace AtEase.App.ViewModels;

public sealed class AppPickerViewModel : ViewModelBase
{
    private AppPickerEntry? _selectedEntry;
    private string _statusMessage = "Select an app and choose Select.";

    public ObservableCollection<AppPickerEntry> Entries { get; }

    public AppPickerEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public AppPickerViewModel(IReadOnlyList<AppPickerEntry> entries)
    {
        Entries = new ObservableCollection<AppPickerEntry>(entries);
    }
}
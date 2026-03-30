using AtEase.App.Models;
using AtEase.App.Services;
using AtEase.App.ViewModels;
using Microsoft.UI.Xaml;

namespace AtEase.App;

public sealed partial class AppPickerWindow : Window
{
    private readonly TaskCompletionSource<AppPickResult> _selectionCompletionSource = new();
    private bool _resultSubmitted;

    public AppPickerViewModel ViewModel { get; }

    public AppPickerWindow(IReadOnlyList<AppPickerEntry> entries)
    {
        ViewModel = new AppPickerViewModel(entries);

        InitializeComponent();

        if (Content is FrameworkElement element)
        {
            element.DataContext = ViewModel;
        }

        Closed += OnClosed;
    }

    public Task<AppPickResult> PickAsync()
    {
        Activate();
        return _selectionCompletionSource.Task;
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedEntry is null)
        {
            ViewModel.StatusMessage = "Select one app from the list.";
            return;
        }

        _resultSubmitted = true;
        _selectionCompletionSource.TrySetResult(AppPickResult.Success(ViewModel.SelectedEntry));
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _resultSubmitted = true;
        _selectionCompletionSource.TrySetResult(AppPickResult.Cancelled());
        Close();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_resultSubmitted)
        {
            return;
        }

        _selectionCompletionSource.TrySetResult(AppPickResult.Cancelled());
    }
}
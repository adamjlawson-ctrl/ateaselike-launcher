using AtEase.App.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AtEase.App;

public sealed partial class BrowseProofPickerWindow : Window
{
    private readonly TaskCompletionSource<AppPickerEntry?> _completion = new();
    private bool _resultSet;

    public BrowseProofPickerWindow(IReadOnlyList<AppPickerEntry> entries)
    {
        InitializeComponent();

        TestListView.ItemsSource = entries;
        if (entries.Count == 0)
        {
            NoAppsText.Visibility = Visibility.Visible;
            SelectButton.IsEnabled = false;
        }

        Closed += OnClosed;
    }

    public Task<AppPickerEntry?> PickAsync()
    {
        Activate();
        return _completion.Task;
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        if (TestListView.SelectedItem is not AppPickerEntry selected)
        {
            NoAppsText.Text = "Select an app";
            NoAppsText.Visibility = Visibility.Visible;
            return;
        }

        _resultSet = true;
        _completion.TrySetResult(selected);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _resultSet = true;
        _completion.TrySetResult(null);
        Close();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_resultSet)
        {
            return;
        }

        _completion.TrySetResult(null);
    }
}

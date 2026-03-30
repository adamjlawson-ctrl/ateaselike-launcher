using Microsoft.UI.Xaml.Controls;
using AtEase.App.Models;
using AtEase.App.ViewModels;
using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace AtEase.App.Views;

public sealed partial class TileSettingsView : UserControl
{
    public TileSettingsView()
    {
        InitializeComponent();
    }

    private void BrowseAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (DataContext is not TileSettingsViewModel viewModel)
        {
            return;
        }

        if (element.DataContext is not AppItem item)
        {
            return;
        }

        // Keep direct click proof in place while running the real browse flow.
        viewModel.SetStatusMessageForDiagnostics("Browse click fired");
        Debug.WriteLine("[AtEase][Settings] Browse click fired.");

        viewModel.BrowseAppPathCommand.Execute(item);
    }
}
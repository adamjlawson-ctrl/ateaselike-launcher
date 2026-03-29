using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Diagnostics;
using System.Linq;
using AtEase.App.Models;
using AtEase.App.ViewModels;

namespace AtEase.App.Views;

public sealed partial class LauncherView : UserControl
{
    public LauncherView()
    {
        InitializeComponent();
    }

    private void OpenMenuFlyout_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            FlyoutBase.ShowAttachedFlyout(element);
        }
    }

    private async void EjectRemovableMediaMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LauncherViewModel viewModel)
        {
            return;
        }

        var drives = viewModel.RemovableMediaPanels.ToList();
        if (drives.Count == 0)
        {
            viewModel.EjectRemovableMediaCommand.Execute(null);
            return;
        }

        if (drives.Count == 1)
        {
            viewModel.EjectRemovableMediaByPanel(drives[0]);
            return;
        }

        var picker = new ComboBox
        {
            ItemsSource = drives,
            SelectedIndex = 0,
            DisplayMemberPath = nameof(RemovableMediaPanel.Title),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 320
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Eject Removable Media",
            Content = picker,
            PrimaryButtonText = "Eject",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        if (picker.SelectedItem is RemovableMediaPanel selectedPanel)
        {
            viewModel.EjectRemovableMediaByPanel(selectedPanel);
        }
    }

    private void ApplicationIndicator_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement anchor)
        {
            return;
        }

        if (DataContext is not LauncherViewModel viewModel)
        {
            return;
        }

        viewModel.RefreshApplicationTrackingState();

        var flyout = new MenuFlyout();

        var atEaseItem = new MenuFlyoutItem
        {
            Text = "At Ease"
        };
        atEaseItem.Click += (_, _) => viewModel.SwitchToAtEase();
        flyout.Items.Add(atEaseItem);

        var trackedApps = viewModel.TrackedApplications
            .OrderBy(app => app.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (trackedApps.Count > 0)
        {
            flyout.Items.Add(new MenuFlyoutSeparator());

            foreach (var app in trackedApps)
            {
                var appMenuItem = new MenuFlyoutItem
                {
                    Text = app.MenuLabel
                };

                appMenuItem.Click += (_, _) => viewModel.SwitchToTrackedApplication(app);
                flyout.Items.Add(appMenuItem);
            }
        }

        flyout.ShowAt(anchor);
    }

    private void FolderRootItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LauncherViewModel viewModel)
        {
            return;
        }

        if (sender is not FrameworkElement element || element.DataContext is not FolderItem folder)
        {
            return;
        }

        Debug.WriteLine($"[AtEase] Root folder click: {folder.DisplayName} | {folder.Path}");
        viewModel.FolderTileClickCommand.Execute(folder);
    }

    private async void AppItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LauncherViewModel viewModel)
        {
            return;
        }

        if (sender is not FrameworkElement element || element.DataContext is not AppItem app)
        {
            return;
        }

        Debug.WriteLine($"[AtEase] App click: {app.DisplayName} | {app.Path}");

        var displays = viewModel.GetAvailableDisplays();
        if (displays.Count <= 1)
        {
            viewModel.LaunchAppOnDisplay(app, null);
            return;
        }

        var picker = new ListView
        {
            ItemsSource = displays,
            SelectionMode = ListViewSelectionMode.Single,
            SelectedIndex = 0,
            IsItemClickEnabled = false,
            MinHeight = 160,
            MaxHeight = 260,
            Width = 420
        };

        picker.ItemTemplate = BuildDisplayPickerTemplate();

        var dialogContent = new StackPanel
        {
            Spacing = 8
        };
        dialogContent.Children.Add(new TextBlock
        {
            Text = "Choose display for this app launch:",
            FontSize = 13
        });
        dialogContent.Children.Add(picker);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Open App",
            Content = dialogContent,
            PrimaryButtonText = "Open",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        if (picker.SelectedItem is not DisplayTarget selectedDisplay)
        {
            selectedDisplay = displays[0];
        }

        viewModel.LaunchAppOnDisplay(app, selectedDisplay);
    }

    private static DataTemplate BuildDisplayPickerTemplate()
    {
        var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
    <TextBlock Text='{Binding PickerLabel}' FontSize='12' />
</DataTemplate>";

        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
    }

    private void FolderBrowserItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LauncherViewModel viewModel)
        {
            return;
        }

        if (sender is not FrameworkElement element || element.DataContext is not MediaPanelItem item)
        {
            return;
        }

        Debug.WriteLine($"[AtEase] Folder-browser click: {item.DisplayName} | {item.Path} | IsFolder={item.IsFolder}");
        viewModel.MediaItemClickCommand.Execute(item);
    }

    private void RemovableMediaItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LauncherViewModel viewModel)
        {
            return;
        }

        if (sender is not FrameworkElement element || element.DataContext is not MediaPanelItem item)
        {
            return;
        }

        Debug.WriteLine($"[AtEase] Removable-media click: {item.DisplayName} | {item.Path} | IsFolder={item.IsFolder}");
        viewModel.MediaItemClickCommand.Execute(item);
    }
}

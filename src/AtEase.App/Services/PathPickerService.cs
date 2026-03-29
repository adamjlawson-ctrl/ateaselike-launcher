using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AtEase.App.Services;

public class PathPickerService
{
    private readonly WindowHandleService _windowHandleService;

    public PathPickerService(WindowHandleService windowHandleService)
    {
        _windowHandleService = windowHandleService;
    }

    public async Task<PathPickResult> PickAppExecutablePathAsync()
    {
        var hwnd = _windowHandleService.CurrentWindowHandle;
        if (hwnd == 0)
        {
            return PathPickResult.Failure("Could not open app picker right now.");
        }

        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };

            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".bat");
            picker.FileTypeFilter.Add(".cmd");

            InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return PathPickResult.Cancelled();
            }

            return PathPickResult.Success(file.Path);
        }
        catch
        {
            return PathPickResult.Failure("Could not open app picker.");
        }
    }

    public async Task<PathPickResult> PickFolderPathAsync()
    {
        var hwnd = _windowHandleService.CurrentWindowHandle;
        if (hwnd == 0)
        {
            return PathPickResult.Failure("Could not open folder picker right now.");
        }

        try
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };

            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return PathPickResult.Cancelled();
            }

            return PathPickResult.Success(folder.Path);
        }
        catch
        {
            return PathPickResult.Failure("Could not open folder picker.");
        }
    }
}
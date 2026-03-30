using AtEase.App.Models;

namespace AtEase.App.Services;

public sealed class AppPickerService
{
    private readonly CuratedAppDiscoveryService _discoveryService;

    public AppPickerService(CuratedAppDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    public async Task<AppPickResult> PickAppAsync(IReadOnlyCollection<string> excludedLaunchPaths, Action<string>? diagnosticsCallback = null)
    {
        IReadOnlyList<AppPickerEntry> candidates;
        try
        {
            candidates = _discoveryService.DiscoverCandidates(excludedLaunchPaths, diagnosticsCallback);
        }
        catch
        {
            return AppPickResult.Failure("Could not load app list right now.");
        }

        try
        {
            var pickerWindow = new BrowseProofPickerWindow(candidates);
            var selected = await pickerWindow.PickAsync();
            if (selected is null)
            {
                return AppPickResult.Cancelled();
            }

            return AppPickResult.Success(selected);
        }
        catch
        {
            return AppPickResult.Failure("Could not open app picker window.");
        }
    }
}
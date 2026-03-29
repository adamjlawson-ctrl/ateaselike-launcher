using AtEase.App.Models;
using AtEase.App.Services.Interfaces;

namespace AtEase.App.Services.Implementations;

public class ValidationService : IValidationService
{
    public IReadOnlyList<string> ValidateSettings(AppSettings settings)
    {
        var errors = new List<string>();

        if (settings.Apps.Any(a => string.IsNullOrWhiteSpace(a.DisplayName) || string.IsNullOrWhiteSpace(a.Path)))
        {
            errors.Add("Each app item must include a display name and path.");
        }

        if (settings.Folders.Any(f => string.IsNullOrWhiteSpace(f.DisplayName) || string.IsNullOrWhiteSpace(f.Path)))
        {
            errors.Add("Each folder item must include a display name and path.");
        }

        var duplicateIds = settings.Apps
            .Select(a => a.Id)
            .Concat(settings.Folders.Select(f => f.Id))
            .GroupBy(id => id)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            errors.Add("Launcher items contain duplicate IDs.");
        }

        return errors;
    }
}

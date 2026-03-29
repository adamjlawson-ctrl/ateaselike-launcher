using AtEase.App.Models;

namespace AtEase.App.Services.Interfaces;

public interface IValidationService
{
    IReadOnlyList<string> ValidateSettings(AppSettings settings);
}
